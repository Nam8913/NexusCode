# Nexus Code Intelligence Platform - Domain Model

## Entity Overview

```
Repository ─┬─ Project ─┬─ Solution
             │            ├─ SourceFile ─┬─ Namespace
             │            │              ├─ Type (Class/Struct/Interface/Enum)
             │            │              └─ Using Directives
             │            └─ Dependencies
             ├─ Symbol Table ─┬─ Symbols
             │                 └─ References
             └─ Knowledge Graph ─┬─ Graph Nodes
                                  └─ Graph Edges
```

---

## 1. Repository

**Responsibility**: Represents a Git repository being analyzed.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| Name | string | Repository name |
| RootPath | string | Local filesystem path |
| RemoteUrl | string? | Git remote URL |
| DefaultBranch | string | Default branch name |
| Language | string | Primary language |
| FileCount | int | Total source files |
| SymbolCount | int | Total symbols extracted |
| GraphNodeCount | int | Total graph nodes |
| GraphEdgeCount | int | Total graph edges |
| IndexedAt | DateTimeOffset | Last full index time |
| Version | string | Git HEAD commit SHA |
| Status | IndexStatus | Current indexing status |

**Relationships**:
- Has many `Project`s
- Has one `SymbolTable`
- Has one `KnowledgeGraph`

**Lifecycle**:
1. Created when repository is added for indexing
2. Updated as files are scanned and analyzed
3. Status transitions: Pending → Indexing → Ready → Error
4. Deleted when repository is removed

---

## 2. Project

**Responsibility**: Represents a .csproj project within a repository.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| Name | string | Project name |
| FilePath | string | Path to .csproj |
| OutputType | string | Library, Exe, WinExe |
| TargetFramework | string | e.g., net10.0 |
| RootNamespace | string | Default namespace |
| AssemblyName | string | Output assembly name |
| IsUnityProject | bool | Contains Unity references |
| NuGetPackages | List\<string\> | Package references |
| ProjectReferences | List\<Guid\> | Referenced projects |

**Relationships**:
- Belongs to `Repository`
- Contains many `SourceFile`s
- Has many `Dependency` edges to other Projects
- Maps to one Roslyn `Project`

**Lifecycle**:
1. Created during solution parsing
2. Populated with file list from .csproj
3. Analysis triggers Roslyn compilation
4. Updated with symbol/graph data

---

## 3. Solution

**Responsibility**: Represents a .sln file grouping multiple projects.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| Name | string | Solution name |
| FilePath | string | Path to .sln |
| ProjectIds | List\<Guid\> | Projects in solution |

**Relationships**:
- Belongs to `Repository`
- Contains many `Project`s

---

## 4. Document

**Responsibility**: Represents a source code document (file) tracked by Roslyn.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| ProjectId | Guid | Parent project |
| FilePath | string | Relative file path |
| FileName | string | File name |
| Extension | string | File extension |
| ContentHash | string | SHA256 of content |
| Size | long | File size in bytes |
| Encoding | string | File encoding |
| LastModified | DateTimeOffset | Last modification time |
| IsGenerated | bool | Auto-generated file |

**Relationships**:
- Belongs to `Project`
- Contains many `Namespace`s
- Contains many `Type`s
- Maps to Roslyn `Document`

**Lifecycle**:
1. Created when file is discovered
2. Hash computed for change detection
3. Parsed into Syntax Tree
4. Semantic Model generated
5. Re-analyzed when hash changes

---

## 5. SourceFile

**Responsibility**: Lightweight file representation for the graph.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| ProjectId | Guid | Parent project |
| FilePath | string | Relative path |
| Hash | string | SHA256 content hash |
| Size | long | File size in bytes |
| LineCount | int | Number of lines |
| SymbolCount | int | Symbols defined in file |
| LastIndexed | DateTimeOffset | Last analysis time |

**Relationships**:
- Belongs to `Repository` and `Project`
- Source of `DECLARES` edges to Types/Methods
- Target of `REFERENCES` edges from other symbols

---

## 6. Namespace

**Responsibility**: Represents a C# namespace.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| Name | string | Full namespace name |
| FilePath | string | Primary defining file |

**Relationships**:
- Belongs to `Repository`
- Contains many `Type`s
- Maps to Namespace node in graph

---

## 7. Type (Class/Struct/Interface/Enum)

**Responsibility**: Represents a C# type definition.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| ProjectId | Guid | Parent project |
| DocumentId | Guid | Source file |
| NamespaceId | Guid | Namespace |
| Name | string | Type name |
| FullName | string | Fully qualified name |
| Kind | TypeKind | Class, Struct, Interface, Enum, Record |
| AccessModifier | AccessModifier | Public, Internal, Private, etc. |
| IsAbstract | bool | Abstract type |
| IsSealed | bool | Sealed type |
| IsStatic | bool | Static type |
| IsPartial | bool | Partial type |
| IsGeneric | bool | Has type parameters |
| TypeParameters | List\<string\> | Generic type parameter names |
| BaseTypeId | Guid? | Base type (class inheritance) |
| ImplementedInterfaceIds | List\<Guid\> | Implemented interfaces |
| DeclaringTypeId | Guid? | Nested parent type |
| XmlDoc | string? | XML documentation |
| AttributeNames | List\<string\> | Applied attributes |
| StartLine | int | Definition start line |
| EndLine | int | Definition end line |
| CodeHash | string | Hash of type body |

**Relationships**:
- Belongs to `Namespace`, `Project`, `Document`
- Has many `Method`s, `Property`s, `Field`s, `Event`s
- Inherits from another `Type` (INHERITS edge)
- Implements `Interface`s (IMPLEMENTS edge)
- Contains nested `Type`s (CONTAINS edge)

**Lifecycle**:
1. Created during semantic analysis
2. Relationships built as interfaces/base types resolved
3. Updated when source changes
4. Removed when source file deleted

---

## 8. Class

**Responsibility**: Extends Type with class-specific fields.

| Field | Type | Description |
|-------|------|-------------|
| TypeId | Guid | Links to Type entity |
| IsAbstract | bool | Abstract class |
| IsSealed | bool | Sealed class |
| IsStatic | bool | Static class |
| Constructors | List\<Guid\> | Constructor methods |
| IsMonoBehaviour | bool | Unity: inherits MonoBehaviour |
| IsScriptableObject | bool | Unity: inherits ScriptableObject |

---

## 9. Struct

**Responsibility**: Extends Type with struct-specific fields.

| Field | Type | Description |
|-------|------|-------------|
| TypeId | Guid | Links to Type entity |
| IsReadOnly | bool | readonly struct |
| IsRefStruct | bool | ref struct |
| IsRecordStruct | bool | record struct |
| ImplementsInterfaces | List\<Guid\> | Interface implementations |

---

## 10. Interface

**Responsibility**: Extends Type with interface-specific fields.

| Field | Type | Description |
|-------|------|-------------|
| TypeId | Guid | Links to Type entity |
| TypeParameters | List\<string\> | Generic constraints |
| MethodIds | List\<Guid\> | Declared methods |
| PropertyIds | List\<Guid\> | Declared properties |

---

## 11. Enum

**Responsibility**: Extends Type with enum-specific fields.

| Field | Type | Description |
|-------|------|-------------|
| TypeId | Guid | Links to Type entity |
| UnderlyingType | string | byte, sbyte, int, uint, etc. |
| Members | List\<EnumMember\> | Enum values |

---

## 12. Method

**Responsibility**: Represents a method, constructor, or operator.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| TypeId | Guid | Declaring type |
| Name | string | Method name |
| FullName | string | Fully qualified name |
| Kind | MethodKind | Regular, Constructor, Static, Extension, Operator |
| AccessModifier | AccessModifier | Visibility |
| IsStatic | bool | Static method |
| IsVirtual | bool | Virtual method |
| IsAbstract | bool | Abstract method |
| IsOverride | bool | Override method |
| IsAsync | bool | Async method |
| IsGeneric | bool | Has type parameters |
| ReturnType | string | Return type full name |
| Parameters | List\<Parameter\> | Method parameters |
| OverriddenMethodId | Guid? | Method being overridden |
| ImplementsMethodId | Guid? | Interface method implemented |
| BodyHash | string | Hash of method body |
| LineCount | int | Method body line count |
| XmlDoc | string? | XML documentation |
| StartLine | int | Method start line |
| EndLine | int | Method end line |

**Relationships**:
- Belongs to `Type`
- Has many `Parameter`s
- Calls other Methods (CALLS edge)
- Overrides other Methods (OVERRIDES edge)
- Implements interface methods (IMPLEMENTS edge)

---

## 13. Property

**Responsibility**: Represents a property definition.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| TypeId | Guid | Declaring type |
| Name | string | Property name |
| FullName | string | Fully qualified name |
| TypeName | string | Property type |
| AccessModifier | AccessModifier | Visibility |
| IsStatic | bool | Static property |
| IsVirtual | bool | Virtual property |
| IsOverride | bool | Override property |
| HasGetter | bool | Has get accessor |
| HasSetter | bool | Has set accessor |
| IsReadOnly | bool | Get-only property |
| IsRequired | bool | required keyword |
| XmlDoc | string? | XML documentation |

---

## 14. Field

**Responsibility**: Represents a field definition.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| TypeId | Guid | Declaring type |
| Name | string | Field name |
| FullName | string | Fully qualified name |
| TypeName | string | Field type |
| AccessModifier | AccessModifier | Visibility |
| IsStatic | bool | Static field |
| IsReadOnly | bool | readonly field |
| IsConst | bool | const field |
| IsVolatile | bool | volatile field |
| IsSerialized | bool | Unity: [SerializeField] |
| DefaultValue | string? | Default value expression |

---

## 15. Event

**Responsibility**: Represents an event definition.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| TypeId | Guid | Declaring type |
| Name | string | Event name |
| FullName | string | Fully qualified name |
| TypeName | string | Event handler type |
| AccessModifier | AccessModifier | Visibility |
| IsStatic | bool | Static event |
| IsVirtual | bool | Virtual event |
| IsOverride | bool | Override event |

---

## 16. Parameter

**Responsibility**: Represents a method parameter.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| MethodId | Guid | Declaring method |
| Name | string | Parameter name |
| TypeName | string | Parameter type |
| HasDefault | bool | Has default value |
| DefaultValue | string? | Default value |
| IsParams | bool | params keyword |
| IsThis | bool | this keyword (extension) |
| IsRef | bool | ref keyword |
| IsOut | bool | out keyword |
| IsIn | bool | in keyword |

---

## 17. Symbol

**Responsibility**: Unified representation of any code symbol.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| Name | string | Symbol name |
| FullName | string | Fully qualified name |
| Kind | SymbolKind | Method, Property, Field, Type, etc. |
| ContainerId | Guid? | Parent symbol |
| DeclaringTypeId | Guid? | Declaring type |
| TypeId | string | Symbol's type |
| FilePath | string | Source file path |
| StartLine | int | Start line |
| EndLine | int | End line |
| IsDefinition | bool | Is this a definition |
| MetadataToken | int | Roslyn metadata token |

**Relationships**:
- Maps 1:1 to Type, Method, Property, Field, Event
- Has many `Reference`s pointing to it
- Is a node in the Knowledge Graph

---

## 18. Reference

**Responsibility**: Represents a reference to a symbol from source code.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| SymbolId | Guid | Referenced symbol |
| SourceFileId | Guid | File containing reference |
| SourceSymbolId | Guid? | Symbol containing the reference |
| Line | int | Line number |
| Column | int | Column number |
| Kind | ReferenceKind | Read, Write, Call, Type, etc. |
| Context | string | Surrounding code snippet |

**Relationships**:
- References a `Symbol`
- Located in a `SourceFile`
- Within a `SourceSymbol` scope

---

## 19. Dependency

**Responsibility**: Represents a dependency between projects.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| SourceProjectId | Guid | Dependent project |
| TargetProjectId | Guid | Required project |
| Kind | DependencyKind | ProjectReference, PackageReference, AssemblyReference |
| Version | string? | Package version |
| IsTransitive | bool | Transitive dependency |

---

## 20. Chunk

**Responsibility**: A unit of code prepared for embedding.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RepositoryId | Guid | Parent repository |
| ProjectId | Guid | Parent project |
| SourceFileId | Guid | Source file |
| SymbolId | Guid? | Associated symbol |
| Content | string | Text content to embed |
| ChunkType | ChunkType | File, Type, Method, Semantic, GraphContext |
| Metadata | Dictionary\<string, string\> | Searchable metadata |
| ContentHash | string | SHA256 of content |
| TokenCount | int | Estimated token count |
| StartLine | int | Start line in source |
| EndLine | int | End line in source |
| EmbeddingId | Guid? | Reference to embedding |

**Relationships**:
- Created from `SourceFile`
- Optionally linked to `Symbol`
- Generates one `Embedding`

---

## 21. Embedding

**Responsibility**: Vector embedding for a code chunk.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| ChunkId | Guid | Source chunk |
| Model | string | Embedding model name |
| Vector | float[] | Embedding vector |
| VectorId | string | Qdrant point ID |
| Dimension | int | Vector dimension |
| GeneratedAt | DateTimeOffset | Generation time |
| Version | int | Embedding version (model upgrade) |

---

## 22. ContextWindow

**Responsibility**: A prepared context window for LLM consumption.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| Query | string | Original question |
| Symbols | List\<Guid\> | Relevant symbols |
| Chunks | List\<Guid\> | Relevant chunks |
| GraphNodes | List\<Guid\> | Graph nodes included |
| GraphEdges | List\<Guid\> | Graph edges included |
| TotalTokens | int | Token count |
| MaxTokens | int | Maximum allowed tokens |
| Strategy | ContextStrategy | Symbol, Graph, Vector, Hybrid |

---

## 23. AgentRequest

**Responsibility**: An incoming request from an AI agent.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| Tool | string | MCP tool name |
| Arguments | Dictionary\<string, object\> | Tool arguments |
| SessionId | string? | MCP session ID |
| Timestamp | DateTimeOffset | Request time |
| Timeout | TimeSpan | Request timeout |

---

## 24. AgentResponse

**Responsibility**: Response sent back to the AI agent.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Unique identifier |
| RequestId | Guid | Matching request |
| IsError | bool | Error response |
| Content | string | Response content |
| Metadata | Dictionary\<string, object\> | Additional data |
| Duration | TimeSpan | Processing time |
| TokensUsed | int? | Token count for the response |

---

## Enums

```csharp
enum IndexStatus { Pending, Indexing, Ready, Error, Reindexing }
enum TypeKind { Class, Struct, Interface, Enum, Record, Delegate }
enum MethodKind { Regular, Constructor, Destructor, Static, Extension, Operator, Conversion }
enum SymbolKind { Type, Method, Property, Field, Event, Parameter, Namespace, Local }
enum AccessModifier { Public, Internal, Protected, Private, ProtectedInternal, PrivateProtected }
enum ReferenceKind { Read, Write, Call, Type, Attribute, Base, Constraint }
enum DependencyKind { ProjectReference, PackageReference, AssemblyReference }
enum ChunkType { File, Type, Method, Semantic, GraphContext }
enum ContextStrategy { SymbolOnly, GraphOnly, VectorOnly, SymbolGraph, Hybrid, GraphRAG }
```

---

## Entity Relationship Summary

```
Repository 1──N Project 1──N SourceFile 1──N Type 1──N Method
    │                          │                   │──N Property
    │                          │                   │──N Field
    │                          │                   │──N Event
    │                          │
    │                          └──N Document
    │
    ├──1 SymbolTable 1──N Symbol 1──N Reference
    │
    └──1 KnowledgeGraph 1──N GraphNode
                               1──N GraphEdge

SourceFile 1──N Chunk 1──1 Embedding
Project 1──N Dependency──1 Project
```
