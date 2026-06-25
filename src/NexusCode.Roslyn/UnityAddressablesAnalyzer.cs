using System.Text.RegularExpressions;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class UnityAddressablesAnalyzer
{
    public AddressablesInfo AnalyzeRepository(string repositoryPath)
    {
        var info = new AddressablesInfo();

        var settingsPath = Path.Combine(repositoryPath, "Assets", "AddressableAssetsData", "AddressableAssetSettings.asset");
        if (File.Exists(settingsPath))
        {
            info.SettingsPath = settingsPath;
            info.IsEnabled = true;
        }

        var groupsPath = Path.Combine(repositoryPath, "Assets", "AddressableAssetsData", "AssetGroups");
        if (Directory.Exists(groupsPath))
        {
            info.GroupFiles = Directory.GetFiles(groupsPath, "*.asset").ToList();
        }

        info.AssetEntries = FindAddressableAssets(repositoryPath);

        return info;
    }

    public List<GraphNodeEntity> BuildAddressableNodes(AddressablesInfo info)
    {
        var nodes = new List<GraphNodeEntity>();

        if (info.IsEnabled)
        {
            nodes.Add(new GraphNodeEntity
            {
                Id = CreateNodeId("Addressables"),
                FullName = "Unity.Addressables",
                Label = "Addressables",
                Kind = NodeKind.Package,
                Metadata = new Dictionary<string, string>
                {
                    ["SettingsPath"] = info.SettingsPath,
                    ["GroupCount"] = info.GroupFiles.Count.ToString(),
                    ["AssetCount"] = info.AssetEntries.Count.ToString()
                }
            });
        }

        foreach (var entry in info.AssetEntries)
        {
            var nodeId = CreateAssetNodeId(entry.AssetPath);
            nodes.Add(new GraphNodeEntity
            {
                Id = nodeId,
                FullName = entry.AssetPath,
                Label = Path.GetFileNameWithoutExtension(entry.AssetPath),
                Kind = NodeKind.File,
                Metadata = new Dictionary<string, string>
                {
                    ["AddressableKey"] = entry.Address,
                    ["AssetPath"] = entry.AssetPath,
                    ["GroupName"] = entry.GroupName
                }
            });
        }

        return nodes;
    }

    public List<GraphEdgeEntity> BuildAddressableEdges(AddressablesInfo info, Dictionary<string, byte[]> scriptNameToId)
    {
        var edges = new List<GraphEdgeEntity>();
        var addressablesNodeId = CreateNodeId("Addressables");

        foreach (var entry in info.AssetEntries)
        {
            var assetNodeId = CreateAssetNodeId(entry.AssetPath);

            edges.Add(new GraphEdgeEntity
            {
                Id = ComputeEdgeId(addressablesNodeId, assetNodeId, EdgeKind.AddressableRef),
                SourceId = addressablesNodeId,
                TargetId = assetNodeId,
                Kind = EdgeKind.AddressableRef,
                Metadata = new Dictionary<string, string>
                {
                    ["Address"] = entry.Address,
                    ["GroupName"] = entry.GroupName
                }
            });

            if (entry.AssetPath.EndsWith(".cs") && scriptNameToId.TryGetValue(Path.GetFileNameWithoutExtension(entry.AssetPath), out var scriptId))
            {
                edges.Add(new GraphEdgeEntity
                {
                    Id = ComputeEdgeId(assetNodeId, scriptId, EdgeKind.References),
                    SourceId = assetNodeId,
                    TargetId = scriptId,
                    Kind = EdgeKind.References
                });
            }
        }

        return edges;
    }

    private List<AddressableAssetEntry> FindAddressableAssets(string repositoryPath)
    {
        var entries = new List<AddressableAssetEntry>();

        var contentPath = Path.Combine(repositoryPath, "Assets", "AddressableAssetsData");
        if (!Directory.Exists(contentPath))
            return entries;

        var assetFiles = Directory.GetFiles(contentPath, "*.asset", SearchOption.AllDirectories);
        foreach (var file in assetFiles)
        {
            if (file.Contains("AssetGroups") || file.Contains("AssetGroupSchema")) continue;

            var content = File.ReadAllText(file);
            var guidMatches = Regex.Matches(content, @"m_AssetGuid:\s*([a-f0-9]+)");
            var pathMatches = Regex.Matches(content, @"m_AssetPath:\s*(.*)");

            for (int i = 0; i < guidMatches.Count; i++)
            {
                entries.Add(new AddressableAssetEntry
                {
                    Guid = guidMatches[i].Groups[1].Value,
                    AssetPath = i < pathMatches.Count ? pathMatches[i].Groups[1].Value.Trim('"') : "",
                    Address = Path.GetFileNameWithoutExtension(file),
                    GroupName = "Default"
                });
            }
        }

        return entries;
    }

    private static byte[] CreateNodeId(string name)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"Addressable:{name}"));
        return hash[..16];
    }

    private static byte[] CreateAssetNodeId(string path)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"AddressableAsset:{path}"));
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

public class AddressablesInfo
{
    public bool IsEnabled { get; set; }
    public string SettingsPath { get; set; } = string.Empty;
    public List<string> GroupFiles { get; set; } = [];
    public List<AddressableAssetEntry> AssetEntries { get; set; } = [];
}

public class AddressableAssetEntry
{
    public string Guid { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
}
