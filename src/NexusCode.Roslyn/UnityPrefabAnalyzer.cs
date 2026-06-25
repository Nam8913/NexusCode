using System.Text.RegularExpressions;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class UnityPrefabAnalyzer
{
    public UnityPrefabInfo AnalyzePrefab(string prefabPath)
    {
        var info = new UnityPrefabInfo
        {
            FilePath = prefabPath,
            Name = Path.GetFileNameWithoutExtension(prefabPath)
        };

        if (!File.Exists(prefabPath))
            return info;

        var content = File.ReadAllText(prefabPath);
        info.GameObjects = ParsePrefabGameObjects(content);
        info.ComponentCount = info.GameObjects.Sum(g => g.Components.Count);

        return info;
    }

    public List<GraphEdgeEntity> BuildPrefabEdges(UnityPrefabInfo prefab, Dictionary<string, byte[]> scriptNameToId)
    {
        var edges = new List<GraphEdgeEntity>();

        var prefabNodeId = CreatePrefabId(prefab.FilePath);

        foreach (var go in prefab.GameObjects)
        {
            var goNodeId = CreateGameObjectId(prefab.FilePath, go.Name);

            edges.Add(new GraphEdgeEntity
            {
                Id = ComputeEdgeId(goNodeId, prefabNodeId, EdgeKind.InstanceOf),
                SourceId = goNodeId,
                TargetId = prefabNodeId,
                Kind = EdgeKind.InstanceOf
            });

            foreach (var component in go.Components)
            {
                if (scriptNameToId.TryGetValue(component.TypeName, out var scriptId))
                {
                    edges.Add(new GraphEdgeEntity
                    {
                        Id = ComputeEdgeId(goNodeId, scriptId, EdgeKind.Component),
                        SourceId = goNodeId,
                        TargetId = scriptId,
                        Kind = EdgeKind.Component
                    });
                }
            }
        }

        return edges;
    }

    public List<GraphNodeEntity> BuildPrefabNodes(UnityPrefabInfo prefab)
    {
        var nodes = new List<GraphNodeEntity>();

        nodes.Add(new GraphNodeEntity
        {
            Id = CreatePrefabId(prefab.FilePath),
            FullName = prefab.FilePath,
            Label = prefab.Name,
            Kind = NodeKind.Prefab,
            Metadata = new Dictionary<string, string>
            {
                ["FilePath"] = prefab.FilePath,
                ["GameObjectCount"] = prefab.GameObjects.Count.ToString(),
                ["ComponentCount"] = prefab.ComponentCount.ToString()
            }
        });

        foreach (var go in prefab.GameObjects)
        {
            nodes.Add(new GraphNodeEntity
            {
                Id = CreateGameObjectId(prefab.FilePath, go.Name),
                FullName = $"{prefab.Name}/{go.Name}",
                Label = go.Name,
                Kind = NodeKind.GameObject,
                Metadata = new Dictionary<string, string>
                {
                    ["Prefab"] = prefab.FilePath,
                    ["Components"] = string.Join(",", go.Components.Select(c => c.TypeName))
                }
            });
        }

        return nodes;
    }

    private List<PrefabGameObject> ParsePrefabGameObjects(string content)
    {
        var gameObjects = new List<PrefabGameObject>();
        var lines = content.Split('\n');

        string? currentName = null;
        var currentComponents = new List<PrefabComponent>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("--- !u!1 &"))
            {
                if (currentName != null)
                {
                    gameObjects.Add(new PrefabGameObject
                    {
                        Name = currentName,
                        Components = [.. currentComponents]
                    });
                }

                currentName = "PrefabGO_" + (gameObjects.Count + 1);
                currentComponents = [];
            }

            if (line.StartsWith("m_Name:"))
            {
                var match = Regex.Match(line, @"m_Name:\s*(.*)");
                if (match.Success)
                    currentName = match.Groups[1].Value.Trim('"');
            }

            if (line.Contains("m_Script:"))
            {
                var match = Regex.Match(line, @"m_Script:\s*\{fileID:\s*\d+,\s*guid:\s*([a-f0-9]+)");
                if (match.Success)
                {
                    currentComponents.Add(new PrefabComponent
                    {
                        TypeName = "Script_" + match.Groups[1].Value[..8],
                        ScriptGuid = match.Groups[1].Value
                    });
                }
            }
        }

        if (currentName != null)
        {
            gameObjects.Add(new PrefabGameObject
            {
                Name = currentName,
                Components = [.. currentComponents]
            });
        }

        return gameObjects;
    }

    private static byte[] CreatePrefabId(string path)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"Prefab:{path}"));
        return hash[..16];
    }

    private static byte[] CreateGameObjectId(string prefabPath, string name)
    {
        var key = $"PrefabGO:{prefabPath}:{name}";
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

public class UnityPrefabInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PrefabGameObject> GameObjects { get; set; } = [];
    public int ComponentCount { get; set; }
}

public class PrefabGameObject
{
    public string Name { get; set; } = string.Empty;
    public List<PrefabComponent> Components { get; set; } = [];
}

public class PrefabComponent
{
    public string TypeName { get; set; } = string.Empty;
    public string ScriptGuid { get; set; } = string.Empty;
}
