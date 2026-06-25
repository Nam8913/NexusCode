using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class UnityIntelligenceEngine
{
    private readonly UnityAnalyzer _analyzer;
    private readonly AssemblyDefinitionAnalyzer _asmDefAnalyzer;
    private readonly UnitySceneAnalyzer _sceneAnalyzer;
    private readonly UnityPrefabAnalyzer _prefabAnalyzer;
    private readonly UnityAddressablesAnalyzer _addressablesAnalyzer;
    private readonly UnityEventAnalyzer _eventAnalyzer;

    public UnityIntelligenceEngine()
    {
        _analyzer = new UnityAnalyzer();
        _asmDefAnalyzer = new AssemblyDefinitionAnalyzer();
        _sceneAnalyzer = new UnitySceneAnalyzer();
        _prefabAnalyzer = new UnityPrefabAnalyzer();
        _addressablesAnalyzer = new UnityAddressablesAnalyzer();
        _eventAnalyzer = new UnityEventAnalyzer();
    }

    public UnityProjectInfo AnalyzeProject(string repositoryPath)
    {
        var info = new UnityProjectInfo
        {
            RepositoryPath = repositoryPath,
            IsUnityProject = DetectUnityProject(repositoryPath)
        };

        if (!info.IsUnityProject)
            return info;

        info.Assemblies = _asmDefAnalyzer.AnalyzeRepository(repositoryPath);
        info.Scenes = AnalyzeScenes(repositoryPath);
        info.Prefabs = AnalyzePrefabs(repositoryPath);
        info.Addressables = _addressablesAnalyzer.AnalyzeRepository(repositoryPath);

        return info;
    }

    public KnowledgeGraph BuildUnityGraph(UnityProjectInfo projectInfo, SymbolTable symbolTable)
    {
        var graph = new KnowledgeGraph();

        var asmDefNodes = _asmDefAnalyzer.BuildAssemblyNodes(projectInfo.Assemblies);
        foreach (var node in asmDefNodes)
            graph.AddNode(node);

        var asmDefEdges = _asmDefAnalyzer.BuildAssemblyDependencyEdges(projectInfo.Assemblies);
        foreach (var edge in asmDefEdges)
            graph.AddEdge(edge);

        var scriptNameToId = BuildScriptNameToIdMap(symbolTable);

        foreach (var scene in projectInfo.Scenes)
        {
            var sceneNodes = _sceneAnalyzer.BuildSceneNodes(scene);
            foreach (var node in sceneNodes)
                graph.AddNode(node);

            var sceneEdges = _sceneAnalyzer.BuildSceneEdges(scene, scriptNameToId);
            foreach (var edge in sceneEdges)
                graph.AddEdge(edge);
        }

        foreach (var prefab in projectInfo.Prefabs)
        {
            var prefabNodes = _prefabAnalyzer.BuildPrefabNodes(prefab);
            foreach (var node in prefabNodes)
                graph.AddNode(node);

            var prefabEdges = _prefabAnalyzer.BuildPrefabEdges(prefab, scriptNameToId);
            foreach (var edge in prefabEdges)
                graph.AddEdge(edge);
        }

        var addrNodes = _addressablesAnalyzer.BuildAddressableNodes(projectInfo.Addressables);
        foreach (var node in addrNodes)
            graph.AddNode(node);

        var addrEdges = _addressablesAnalyzer.BuildAddressableEdges(projectInfo.Addressables, scriptNameToId);
        foreach (var edge in addrEdges)
            graph.AddEdge(edge);

        return graph;
    }

    public List<UnityTypeInfo> AnalyzeUnitySymbols(SymbolTable symbolTable)
    {
        var results = new List<UnityTypeInfo>();

        var classes = symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Type)
            .Where(s => s.TypeName == "Class");

        foreach (var classSymbol in classes)
        {
            if (classSymbol.FilePath == null) continue;

            try
            {
                var content = File.ReadAllText(classSymbol.FilePath);
                if (content.Contains("MonoBehaviour") || content.Contains("ScriptableObject") || content.Contains("Editor"))
                {
                    var isMono = content.Contains(": MonoBehaviour") || content.Contains(": UnityEngine.MonoBehaviour");
                    var isSO = content.Contains(": ScriptableObject") || content.Contains(": UnityEngine.ScriptableObject");
                    var isEditor = content.Contains(": Editor") || content.Contains(": UnityEditor.Editor");

                    if (isMono || isSO || isEditor)
                    {
                        results.Add(new UnityTypeInfo
                        {
                            TypeName = classSymbol.Name,
                            FullName = classSymbol.FullName,
                            IsMonoBehaviour = isMono,
                            IsScriptableObject = isSO,
                            IsEditor = isEditor
                        });
                    }
                }
            }
            catch { }
        }

        return results;
    }

    private bool DetectUnityProject(string repositoryPath)
    {
        var projectSettingsPath = Path.Combine(repositoryPath, "ProjectSettings");
        var assetsPath = Path.Combine(repositoryPath, "Assets");

        return Directory.Exists(projectSettingsPath) && Directory.Exists(assetsPath);
    }

    private List<UnitySceneInfo> AnalyzeScenes(string repositoryPath)
    {
        var scenes = new List<UnitySceneInfo>();
        var assetsPath = Path.Combine(repositoryPath, "Assets");

        if (!Directory.Exists(assetsPath))
            return scenes;

        var sceneFiles = Directory.GetFiles(assetsPath, "*.unity", SearchOption.AllDirectories);
        foreach (var sceneFile in sceneFiles)
        {
            scenes.Add(_sceneAnalyzer.AnalyzeScene(sceneFile));
        }

        return scenes;
    }

    private List<UnityPrefabInfo> AnalyzePrefabs(string repositoryPath)
    {
        var prefabs = new List<UnityPrefabInfo>();
        var assetsPath = Path.Combine(repositoryPath, "Assets");

        if (!Directory.Exists(assetsPath))
            return prefabs;

        var prefabFiles = Directory.GetFiles(assetsPath, "*.prefab", SearchOption.AllDirectories);
        foreach (var prefabFile in prefabFiles)
        {
            prefabs.Add(_prefabAnalyzer.AnalyzePrefab(prefabFile));
        }

        return prefabs;
    }

    private Dictionary<string, byte[]> BuildScriptNameToIdMap(SymbolTable symbolTable)
    {
        var map = new Dictionary<string, byte[]>();
        var classes = symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Type)
            .Where(s => s.TypeName == "Class");

        foreach (var cls in classes)
        {
            var hash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(cls.FullName));
            map[cls.Name] = hash[..16];
        }

        return map;
    }
}

public class UnityProjectInfo
{
    public string RepositoryPath { get; set; } = string.Empty;
    public bool IsUnityProject { get; set; }
    public List<AssemblyDefinitionInfo> Assemblies { get; set; } = [];
    public List<UnitySceneInfo> Scenes { get; set; } = [];
    public List<UnityPrefabInfo> Prefabs { get; set; } = [];
    public AddressablesInfo Addressables { get; set; } = new();
}
