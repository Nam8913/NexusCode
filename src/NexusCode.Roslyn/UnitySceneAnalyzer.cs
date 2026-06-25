using System.Text.RegularExpressions;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class UnitySceneAnalyzer
{
    public UnitySceneInfo AnalyzeScene(string scenePath)
    {
        var info = new UnitySceneInfo
        {
            FilePath = scenePath,
            Name = Path.GetFileNameWithoutExtension(scenePath)
        };

        if (!File.Exists(scenePath))
            return info;

        var content = File.ReadAllText(scenePath);
        info.GameObjects = ParseGameObjects(content);
        info.ComponentCount = info.GameObjects.Sum(g => g.Components.Count);

        return info;
    }

    public List<GraphEdgeEntity> BuildSceneEdges(UnitySceneInfo scene, Dictionary<string, byte[]> scriptNameToId)
    {
        var edges = new List<GraphEdgeEntity>();

        foreach (var go in scene.GameObjects)
        {
            var goNodeId = CreateGameObjectId(scene.FilePath, go.Name);

            foreach (var component in go.Components)
            {
                if (scriptNameToId.TryGetValue(component.TypeName, out var scriptId))
                {
                    edges.Add(new GraphEdgeEntity
                    {
                        Id = ComputeEdgeId(goNodeId, scriptId, EdgeKind.Component),
                        SourceId = goNodeId,
                        TargetId = scriptId,
                        Kind = EdgeKind.Component,
                        Metadata = new Dictionary<string, string>
                        {
                            ["Scene"] = scene.FilePath,
                            ["GameObject"] = go.Name
                        }
                    });
                }

                foreach (var dep in component.Dependencies)
                {
                    if (scriptNameToId.TryGetValue(dep, out var depId))
                    {
                        edges.Add(new GraphEdgeEntity
                        {
                            Id = ComputeEdgeId(scriptId, depId, EdgeKind.Requires),
                            SourceId = scriptId,
                            TargetId = depId,
                            Kind = EdgeKind.Requires
                        });
                    }
                }
            }

            if (go.ParentName != null)
            {
                var parentId = CreateGameObjectId(scene.FilePath, go.ParentName);
                edges.Add(new GraphEdgeEntity
                {
                    Id = ComputeEdgeId(goNodeId, parentId, EdgeKind.ChildOf),
                    SourceId = goNodeId,
                    TargetId = parentId,
                    Kind = EdgeKind.ChildOf
                });
            }
        }

        return edges;
    }

    public List<GraphNodeEntity> BuildSceneNodes(UnitySceneInfo scene)
    {
        var nodes = new List<GraphNodeEntity>();

        foreach (var go in scene.GameObjects)
        {
            var nodeId = CreateGameObjectId(scene.FilePath, go.Name);
            nodes.Add(new GraphNodeEntity
            {
                Id = nodeId,
                FullName = $"{scene.Name}/{go.Name}",
                Label = go.Name,
                Kind = NodeKind.GameObject,
                Metadata = new Dictionary<string, string>
                {
                    ["Scene"] = scene.FilePath,
                    ["Components"] = string.Join(",", go.Components.Select(c => c.TypeName)),
                    ["Parent"] = go.ParentName ?? ""
                }
            });
        }

        return nodes;
    }

    private List<SceneGameObject> ParseGameObjects(string content)
    {
        var gameObjects = new List<SceneGameObject>();
        var lines = content.Split('\n');

        string? currentName = null;
        string? currentParent = null;
        var currentComponents = new List<SceneComponent>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("--- !u!1 &"))
            {
                if (currentName != null)
                {
                    gameObjects.Add(new SceneGameObject
                    {
                        Name = currentName,
                        ParentName = currentParent,
                        Components = [.. currentComponents]
                    });
                }

                currentName = "GameObject_" + (gameObjects.Count + 1);
                currentParent = null;
                currentComponents = [];
            }

            if (line.StartsWith("m_Name:"))
            {
                var match = Regex.Match(line, @"m_Name:\s*(.*)");
                if (match.Success)
                    currentName = match.Groups[1].Value.Trim('"');
            }

            if (line.StartsWith("m_Father:"))
            {
                var match = Regex.Match(line, @"m_Father:\s*\{fileID:\s*(\d+)");
                if (match.Success && match.Groups[1].Value != "0")
                    currentParent = "Parent_" + match.Groups[1].Value;
            }

            if (line.Contains("m_Script:"))
            {
                var match = Regex.Match(line, @"m_Script:\s*\{fileID:\s*\d+,\s*guid:\s*([a-f0-9]+)");
                if (match.Success)
                {
                    currentComponents.Add(new SceneComponent
                    {
                        TypeName = "Script_" + match.Groups[1].Value[..8],
                        ScriptGuid = match.Groups[1].Value
                    });
                }
            }
        }

        if (currentName != null)
        {
            gameObjects.Add(new SceneGameObject
            {
                Name = currentName,
                ParentName = currentParent,
                Components = [.. currentComponents]
            });
        }

        return gameObjects;
    }

    private static byte[] CreateGameObjectId(string scenePath, string name)
    {
        var key = $"GameObject:{scenePath}:{name}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        return hash[..16];
    }

    private static byte[] ComputeEdgeId(byte[] source, byte[] target, EdgeKind kind)
    {
        var combined = new byte[source.Length + target.Length + 4];
        source.CopyTo(combined, 0);
        target.CopyTo(combined, source.Length);
        BitConverter.GetBytes((int)kind).CopyTo(combined, source.Length + target.Length);
        return System.Security.Cryptography.MD5.HashData(combined);
    }
}

public class UnitySceneInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<SceneGameObject> GameObjects { get; set; } = [];
    public int ComponentCount { get; set; }
}

public class SceneGameObject
{
    public string Name { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public List<SceneComponent> Components { get; set; } = [];
}

public class SceneComponent
{
    public string TypeName { get; set; } = string.Empty;
    public string ScriptGuid { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = [];
}
