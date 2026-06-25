using NexusCode.Domain;

namespace NexusCode.Roslyn;

public class AnalysisResult
{
    public string FilePath { get; set; } = string.Empty;
    public List<SymbolEntity> Symbols { get; set; } = [];
    public List<ReferenceEntity> References { get; set; } = [];
    public List<GraphNodeEntity> GraphNodes { get; set; } = [];
    public List<GraphEdgeEntity> GraphEdges { get; set; } = [];
}
