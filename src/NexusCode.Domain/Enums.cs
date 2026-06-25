namespace NexusCode.Domain;

public enum IndexStatus
{
    Pending,
    Indexing,
    Ready,
    Error,
    Reindexing
}

public enum TypeKind
{
    Unknown,
    Class,
    Struct,
    Interface,
    Enum,
    Record,
    Delegate
}

public enum MethodKind
{
    Regular,
    Constructor,
    Destructor,
    Static,
    Extension,
    Operator,
    Conversion
}

public enum SymbolKind
{
    Unknown,
    Type,
    Method,
    Property,
    Field,
    Event,
    Parameter,
    Namespace,
    Local
}

public enum AccessModifier
{
    NotApplicable,
    Public,
    Internal,
    Protected,
    Private,
    ProtectedInternal,
    PrivateProtected
}

public enum ReferenceKind
{
    Unknown,
    Read,
    Write,
    Call,
    Type,
    Attribute,
    Base,
    Constraint
}

public enum DependencyKind
{
    ProjectReference,
    PackageReference,
    AssemblyReference
}

public enum ChunkType
{
    File,
    Type,
    Method,
    Semantic,
    GraphContext
}

public enum ContextStrategy
{
    SymbolOnly,
    GraphOnly,
    VectorOnly,
    SymbolGraph,
    Hybrid,
    GraphRAG
}

public enum NodeKind
{
    Unknown,
    Repository,
    Project,
    Namespace,
    Class,
    Struct,
    Interface,
    Enum,
    Record,
    Method,
    Property,
    Field,
    Event,
    File,
    Assembly,
    Package,
    MonoBehaviour,
    ScriptableObject,
    Editor,
    GameObject,
    Prefab,
    Component
}

public enum EdgeKind
{
    Unknown,
    Contains,
    Calls,
    Uses,
    References,
    Inherits,
    Implements,
    Declares,
    DependsOn,
    Overrides,
    Reads,
    Writes,
    Requires,
    Attribute,
    Returns,
    Parameter,
    FieldType,
    PropertyType,
    EventHandler,
    ImplicitlyImplements,
    ChildOf,
    InstanceOf,
    AddressableRef,
    UnityEvent,
    Coroutine,
    AssemblyDep,
    Component
}

public enum FileStatus
{
    New,
    Modified,
    Unchanged,
    Deleted,
    Error
}

public enum ChangeType
{
    New,
    Modified,
    Deleted
}

public enum ScanStatus
{
    Pending,
    Scanning,
    Indexing,
    Complete,
    Failed,
    Paused
}

public enum IndexJobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
