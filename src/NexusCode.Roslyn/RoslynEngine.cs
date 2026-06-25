using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class RoslynEngine : IDisposable
{
    private readonly Dictionary<string, Compilation> _compilations = new();
    private readonly Dictionary<string, List<string>> _projectFiles = new();

    public async Task<SolutionInfo> LoadSolutionAsync(string solutionPath, CancellationToken ct = default)
    {
        var solutionInfo = new SolutionInfo { SolutionPath = solutionPath };

        var content = await File.ReadAllTextAsync(solutionPath, ct);
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Project("))
            {
                var start = trimmed.IndexOf('"') + 1;
                var end = trimmed.LastIndexOf('"');
                if (start > 0 && end > start)
                {
                    var projectPath = trimmed.Substring(start, end - start);
                    var fullPath = Path.Combine(Path.GetDirectoryName(solutionPath)!, projectPath);

                    if (File.Exists(fullPath) && fullPath.EndsWith(".csproj"))
                    {
                        solutionInfo.ProjectPaths.Add(fullPath);
                    }
                }
            }
        }

        return solutionInfo;
    }

    public async Task<ProjectInfo> LoadProjectAsync(string projectPath, CancellationToken ct = default)
    {
        var projectInfo = new ProjectInfo { ProjectPath = projectPath };
        var projectDir = Path.GetDirectoryName(projectPath)!;

        var content = await File.ReadAllTextAsync(projectPath, ct);
        var doc = XDocument.Parse(content);
        var ns = doc.Root?.Name.Namespace ?? "";

        var targetFramework = doc.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value;
        projectInfo.TargetFramework = targetFramework ?? "net10.0";

        var sourceFiles = new List<string>();
        foreach (var csFile in Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories))
        {
            if (!csFile.Contains("bin") && !csFile.Contains("obj"))
            {
                sourceFiles.Add(csFile);
            }
        }
        projectInfo.SourceFiles = sourceFiles;

        _projectFiles[projectPath] = sourceFiles;

        return projectInfo;
    }

    public Compilation BuildCompilation(string projectPath, IEnumerable<string> additionalReferences)
    {
        if (!_projectFiles.TryGetValue(projectPath, out var sourceFiles))
        {
            throw new InvalidOperationException($"Project {projectPath} not loaded. Call LoadProjectAsync first.");
        }

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var file in sourceFiles)
        {
            var sourceText = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(sourceText, path: file);
            syntaxTrees.Add(tree);
        }

        var references = GetDefaultReferences();
        foreach (var refPath in additionalReferences)
        {
            if (File.Exists(refPath))
            {
                references.Add(MetadataReference.CreateFromFile(refPath));
            }
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: Path.GetFileNameWithoutExtension(projectPath),
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _compilations[projectPath] = compilation;
        return compilation;
    }

    public Compilation? GetCompilation(string projectPath)
    {
        return _compilations.TryGetValue(projectPath, out var compilation) ? compilation : null;
    }

    public IReadOnlyList<Compilation> GetAllCompilations()
    {
        return _compilations.Values.ToList().AsReadOnly();
    }

    public AnalysisResult AnalyzeFile(string filePath, Compilation compilation)
    {
        var tree = compilation.SyntaxTrees.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (tree == null)
        {
            var sourceText = File.ReadAllText(filePath);
            tree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);
            compilation = compilation.AddSyntaxTrees(tree);
        }

        var semanticModel = compilation.GetSemanticModel(tree);

        var walker = new SyntaxWalker(semanticModel, filePath);
        walker.Visit(tree.GetRoot());

        return new AnalysisResult
        {
            FilePath = filePath,
            Symbols = walker.Symbols,
            References = walker.References,
            GraphNodes = walker.GraphNodes,
            GraphEdges = walker.GraphEdges
        };
    }

    public AnalysisResult AnalyzeSource(string sourceCode, string filePath, Compilation compilation)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode, path: filePath);
        var semanticModel = compilation.GetSemanticModel(tree);

        var walker = new SyntaxWalker(semanticModel, filePath);
        walker.Visit(tree.GetRoot());

        return new AnalysisResult
        {
            FilePath = filePath,
            Symbols = walker.Symbols,
            References = walker.References,
            GraphNodes = walker.GraphNodes,
            GraphEdges = walker.GraphEdges
        };
    }

    private static List<MetadataReference> GetDefaultReferences()
    {
        var references = new List<MetadataReference>();
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

        var essentialAssemblies = new[]
        {
            "System.Runtime.dll",
            "System.Collections.dll",
            "System.Linq.dll",
            "System.Threading.dll",
            "System.Threading.Tasks.dll",
            "System.IO.dll",
            "System.Text.RegularExpressions.dll",
            "netstandard.dll",
            "mscorlib.dll"
        };

        foreach (var assembly in essentialAssemblies)
        {
            var path = Path.Combine(runtimeDir, assembly);
            if (File.Exists(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        return references;
    }

    public void Dispose()
    {
        _compilations.Clear();
        _projectFiles.Clear();
    }
}

public class SolutionInfo
{
    public string SolutionPath { get; set; } = string.Empty;
    public List<string> ProjectPaths { get; set; } = [];
}

public class ProjectInfo
{
    public string ProjectPath { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public List<string> SourceFiles { get; set; } = [];
}
