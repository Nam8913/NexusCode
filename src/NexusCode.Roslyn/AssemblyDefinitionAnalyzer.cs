using System.Text.Json;
using System.Text.Json.Serialization;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class AssemblyDefinitionAnalyzer
{
    public List<AssemblyDefinitionInfo> AnalyzeRepository(string repositoryPath)
    {
        var results = new List<AssemblyDefinitionInfo>();
        var asmdefFiles = Directory.GetFiles(repositoryPath, "*.asmdef", SearchOption.AllDirectories);

        foreach (var file in asmdefFiles)
        {
            if (file.Contains("Library") || file.Contains("Packages")) continue;

            var info = ParseAsmDef(file);
            if (info != null)
                results.Add(info);
        }

        return results;
    }

    public AssemblyDefinitionInfo? ParseAsmDef(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var asmdef = JsonSerializer.Deserialize<AsmDefJson>(content);
            if (asmdef == null) return null;

            return new AssemblyDefinitionInfo
            {
                Name = asmdef.name ?? "",
                RootNamespace = asmdef.rootNamespace ?? "",
                References = asmdef.references ?? [],
                IncludePlatforms = asmdef.includePlatforms ?? [],
                ExcludePlatforms = asmdef.excludePlatforms ?? [],
                AllowUnsafeCode = asmdef.allowUnsafeCode,
                OverrideReferences = asmdef.overrideReferences,
                PrecompiledReferences = asmdef.precompiledReferences ?? [],
                AutoReferenced = asmdef.autoReferenced,
                DefineConstraints = asmdef.defineConstraints ?? [],
                NoEngineReferences = asmdef.noEngineReferences,
                FilePath = filePath,
                Directory = Path.GetDirectoryName(filePath) ?? ""
            };
        }
        catch
        {
            return null;
        }
    }

    public List<GraphEdgeEntity> BuildAssemblyDependencyEdges(List<AssemblyDefinitionInfo> definitions)
    {
        var edges = new List<GraphEdgeEntity>();

        foreach (var def in definitions)
        {
            var sourceId = CreateNodeId(def.Name);

            foreach (var reference in def.References)
            {
                var targetId = CreateNodeId(reference);
                edges.Add(new GraphEdgeEntity
                {
                    Id = ComputeEdgeId(sourceId, targetId, EdgeKind.AssemblyDep),
                    SourceId = sourceId,
                    TargetId = targetId,
                    Kind = EdgeKind.AssemblyDep
                });
            }
        }

        return edges;
    }

    public List<GraphNodeEntity> BuildAssemblyNodes(List<AssemblyDefinitionInfo> definitions)
    {
        var nodes = new List<GraphNodeEntity>();

        foreach (var def in definitions)
        {
            nodes.Add(new GraphNodeEntity
            {
                Id = CreateNodeId(def.Name),
                FullName = def.Name,
                Label = def.Name,
                Kind = NodeKind.Assembly,
                Metadata = new Dictionary<string, string>
                {
                    ["FilePath"] = def.FilePath,
                    ["References"] = string.Join(",", def.References),
                    ["IncludePlatforms"] = string.Join(",", def.IncludePlatforms),
                    ["ExcludePlatforms"] = string.Join(",", def.ExcludePlatforms),
                    ["AllowUnsafeCode"] = def.AllowUnsafeCode.ToString(),
                    ["AutoReferenced"] = def.AutoReferenced.ToString()
                }
            });
        }

        return nodes;
    }

    private static byte[] CreateNodeId(string name)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"Assembly:{name}"));
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

public class AssemblyDefinitionInfo
{
    public string Name { get; set; } = string.Empty;
    public string RootNamespace { get; set; } = string.Empty;
    public List<string> References { get; set; } = [];
    public List<string> IncludePlatforms { get; set; } = [];
    public List<string> ExcludePlatforms { get; set; } = [];
    public bool AllowUnsafeCode { get; set; }
    public bool OverrideReferences { get; set; }
    public List<string> PrecompiledReferences { get; set; } = [];
    public bool AutoReferenced { get; set; } = true;
    public List<string> DefineConstraints { get; set; } = [];
    public bool NoEngineReferences { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Directory { get; set; } = string.Empty;
}

internal class AsmDefJson
{
    [JsonPropertyName("name")]
    public string? name { get; set; }

    [JsonPropertyName("rootNamespace")]
    public string? rootNamespace { get; set; }

    [JsonPropertyName("references")]
    public List<string>? references { get; set; }

    [JsonPropertyName("includePlatforms")]
    public List<string>? includePlatforms { get; set; }

    [JsonPropertyName("excludePlatforms")]
    public List<string>? excludePlatforms { get; set; }

    [JsonPropertyName("allowUnsafeCode")]
    public bool allowUnsafeCode { get; set; }

    [JsonPropertyName("overrideReferences")]
    public bool overrideReferences { get; set; }

    [JsonPropertyName("precompiledReferences")]
    public List<string>? precompiledReferences { get; set; }

    [JsonPropertyName("autoReferenced")]
    public bool autoReferenced { get; set; } = true;

    [JsonPropertyName("defineConstraints")]
    public List<string>? defineConstraints { get; set; }

    [JsonPropertyName("noEngineReferences")]
    public bool noEngineReferences { get; set; }
}
