# Knowledge Graph Feature

## Purpose

In-memory graph storage for code relationships with fast traversal.

## Requirements

- 15+ edge types (CONTAINS, CALLS, INHERITS, IMPLEMENTS, etc.)
- 20+ node types (Class, Method, Property, etc.)
- BFS/DFS traversal
- Parallel processing support
- JSON export for frontend visualization

## Design

### KnowledgeGraph
- ConcurrentDictionary-based storage
- Adjacency lists for fast traversal
- Multi-index by node kind, edge kind, file path

### Node Types

Repository, Project, Namespace, Class, Struct, Interface, Enum, Record, Method, Property, Field, Event, File, Assembly, Package, MonoBehaviour, ScriptableObject, Editor, GameObject, Prefab, Component

### Edge Types

Contains, Calls, Uses, References, Inherits, Implements, Declares, DependsOn, Overrides, Reads, Writes, Requires, Attribute, Returns, Parameter, FieldType, PropertyType, EventHandler, ImplicitlyImplements, ChildOf, InstanceOf, AddressableRef, UnityEvent, Coroutine, AssemblyDep, Component

## Public APIs

```csharp
void AddNode(GraphNodeEntity node)
void AddEdge(GraphEdgeEntity edge)
GraphNodeEntity? GetNode(byte[] id)
IReadOnlyList<GraphNodeEntity> GetNodesByKind(NodeKind kind)
IReadOnlyList<GraphEdgeEntity> GetOutgoingEdges(byte[] nodeId)
IReadOnlyList<GraphEdgeEntity> GetIncomingEdges(byte[] nodeId)
```

## Current Status

✅ Complete - Full graph with JSON export

## Future Work

- Graph partitioning for >1M nodes
- Persistent graph storage
