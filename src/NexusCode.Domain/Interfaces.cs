namespace NexusCode.Domain;

public interface ISymbolInfo
{
    Guid Id { get; }
    string Name { get; }
    string FullName { get; }
    SymbolKind Kind { get; }
    string? TypeName { get; }
    Guid? ContainerId { get; }
    Guid? DeclaringTypeId { get; }
    string? FilePath { get; }
    int StartLine { get; }
    int EndLine { get; }
    bool IsDefinition { get; }
    AccessModifier AccessModifier { get; }
    Dictionary<string, string> Metadata { get; }
}

public interface IReferenceInfo
{
    Guid Id { get; }
    Guid SymbolId { get; }
    Guid SourceFileId { get; }
    Guid? SourceSymbolId { get; }
    int Line { get; }
    int Column { get; }
    ReferenceKind Kind { get; }
    string? Context { get; }
}

public interface IGraphNodeInfo
{
    byte[] Id { get; }
    string FullName { get; }
    string Label { get; }
    NodeKind Kind { get; }
    Dictionary<string, string> Metadata { get; }
}

public interface IGraphEdgeInfo
{
    byte[] Id { get; }
    byte[] SourceId { get; }
    byte[] TargetId { get; }
    EdgeKind Kind { get; }
    double Weight { get; }
    Dictionary<string, string> Metadata { get; }
}

public interface ISymbolTable
{
    void Add(ISymbolInfo symbol);
    void Update(ISymbolInfo symbol);
    void Remove(Guid id);
    ISymbolInfo? GetById(Guid id);
    ISymbolInfo? GetByFullName(string fullName);
    IReadOnlyList<ISymbolInfo> GetByName(string name);
    IReadOnlyList<ISymbolInfo> GetByKind(SymbolKind kind);
    IReadOnlyList<ISymbolInfo> GetByFile(string filePath);
    IReadOnlyList<ISymbolInfo> GetByContainer(Guid containerId);
    IReadOnlyList<IReferenceInfo> GetReferences(Guid symbolId);
    void AddReference(IReferenceInfo reference);
    void RemoveReferencesByFile(string filePath);
    int Count { get; }
}

public interface IKnowledgeGraph
{
    void AddNode(IGraphNodeInfo node);
    void UpdateNode(IGraphNodeInfo node);
    void RemoveNode(byte[] id);
    IGraphNodeInfo? GetNode(byte[] id);
    IReadOnlyList<IGraphNodeInfo> GetNodesByKind(NodeKind kind);
    IReadOnlyList<IGraphNodeInfo> GetNodesByFile(string filePath);
    
    void AddEdge(IGraphEdgeInfo edge);
    void RemoveEdge(byte[] id);
    IReadOnlyList<IGraphEdgeInfo> GetOutgoingEdges(byte[] nodeId);
    IReadOnlyList<IGraphEdgeInfo> GetIncomingEdges(byte[] nodeId);
    IReadOnlyList<IGraphEdgeInfo> GetEdgesByKind(EdgeKind kind);
    
    int NodeCount { get; }
    int EdgeCount { get; }
}

public interface IFileScanner
{
    Task<ScanResult> ScanAsync(string repositoryPath, ScanOptions options, CancellationToken ct = default);
    Task<ChangeSet> DetectChangesAsync(string repositoryPath, CancellationToken ct = default);
}

public interface IIndexer
{
    Task<IndexResult> IndexAsync(string repositoryPath, IndexOptions options, IProgress<IndexProgress>? progress = null, CancellationToken ct = default);
    Task<IndexResult> IndexFileAsync(string filePath, CancellationToken ct = default);
}

public class ScanResult
{
    public List<string> SourceFiles { get; set; } = [];
    public List<string> ProjectFiles { get; set; } = [];
    public List<string> SolutionFiles { get; set; } = [];
    public int TotalFiles => SourceFiles.Count + ProjectFiles.Count + SolutionFiles.Count;
    public TimeSpan Duration { get; set; }
}

public class ChangeSet
{
    public List<string> NewFiles { get; set; } = [];
    public List<string> ModifiedFiles { get; set; } = [];
    public List<string> DeletedFiles { get; set; } = [];
    public bool HasChanges => NewFiles.Count > 0 || ModifiedFiles.Count > 0 || DeletedFiles.Count > 0;
    public int TotalChanges => NewFiles.Count + ModifiedFiles.Count + DeletedFiles.Count;
}

public class ScanOptions
{
    public string[] ExcludePatterns { get; set; } =
    [
        ".git", "bin", "obj", "node_modules", ".vs", "packages",
        "Library", "Temp", "Logs", "UserSettings"
    ];
    public string[] IncludeExtensions { get; set; } = [".cs", ".csproj", ".sln"];
    public bool IncludeGenerated { get; set; } = false;
    public bool IncludeUnity { get; set; } = true;
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
}

public class IndexOptions
{
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;
    public bool IncrementalOnly { get; set; } = false;
    public TimeSpan? Timeout { get; set; }
}

public class IndexResult
{
    public bool Success { get; set; }
    public int FilesIndexed { get; set; }
    public int SymbolsExtracted { get; set; }
    public int GraphNodesCreated { get; set; }
    public int GraphEdgesCreated { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Error { get; set; }
}

public class IndexProgress
{
    public string Status { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FailedFiles { get; set; }
    public double PercentComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
    public TimeSpan Elapsed { get; set; }
    public TimeSpan? EstimatedRemaining { get; set; }
}
