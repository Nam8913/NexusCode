namespace NexusCode.Domain;

public class RepositoryEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string? RemoteUrl { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public string Language { get; set; } = "csharp";
    public int FileCount { get; set; }
    public int SymbolCount { get; set; }
    public int GraphNodeCount { get; set; }
    public int GraphEdgeCount { get; set; }
    public DateTimeOffset? IndexedAt { get; set; }
    public string? Version { get; set; }
    public IndexStatus Status { get; set; } = IndexStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ProjectEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepositoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string OutputType { get; set; } = "Library";
    public string? TargetFramework { get; set; }
    public string? RootNamespace { get; set; }
    public string? AssemblyName { get; set; }
    public bool IsUnityProject { get; set; }
    public List<string> NuGetPackages { get; set; } = [];
    public List<Guid> ProjectReferences { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class SourceFileEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepositoryId { get; set; }
    public Guid ProjectId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public long Size { get; set; }
    public int LineCount { get; set; }
    public int SymbolCount { get; set; }
    public DateTimeOffset? LastIndexed { get; set; }
    public FileStatus Status { get; set; } = FileStatus.New;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class SymbolEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepositoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public SymbolKind Kind { get; set; }
    public string? TypeName { get; set; }
    public Guid? ContainerId { get; set; }
    public Guid? DeclaringTypeId { get; set; }
    public string? FilePath { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public bool IsDefinition { get; set; } = true;
    public AccessModifier AccessModifier { get; set; }
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public bool IsAsync { get; set; }
    public bool IsGeneric { get; set; }
    public string? ReturnType { get; set; }
    public List<ParameterEntity> Parameters { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = [];
    public string? XmlDoc { get; set; }
    public string? CodeHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ParameterEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool HasDefault { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsParams { get; set; }
    public bool IsThis { get; set; }
    public bool IsRef { get; set; }
    public bool IsOut { get; set; }
    public bool IsIn { get; set; }
}

public class ReferenceEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SymbolId { get; set; }
    public Guid SourceFileId { get; set; }
    public Guid? SourceSymbolId { get; set; }
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public ReferenceKind Kind { get; set; }
    public string? Context { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class DependencyEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceProjectId { get; set; }
    public Guid TargetProjectId { get; set; }
    public DependencyKind Kind { get; set; }
    public string? Version { get; set; }
    public bool IsTransitive { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class GraphNodeEntity
{
    public byte[] Id { get; set; } = [];
    public string FullName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public NodeKind Kind { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class GraphEdgeEntity
{
    public byte[] Id { get; set; } = [];
    public byte[] SourceId { get; set; } = [];
    public byte[] TargetId { get; set; } = [];
    public EdgeKind Kind { get; set; }
    public double Weight { get; set; } = 1.0;
    public Dictionary<string, string> Metadata { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ChunkEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepositoryId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid SourceFileId { get; set; }
    public Guid? SymbolId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ChunkType ChunkType { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];
    public string ContentHash { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
