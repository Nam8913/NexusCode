# Nexus Code Intelligence Platform - Knowledge Graph Engine

## Overview

The Knowledge Graph Engine is the central component of the entire platform. It represents code relationships as a directed graph with typed nodes and edges, enabling powerful code intelligence queries through graph traversal.

---

## 1. Graph Schema

### 1.1 Node Types

| Node Type | Description | Properties |
|-----------|-------------|------------|
| Repository | Git repository | name, rootPath, remoteUrl |
| Project | .csproj project | name, targetFramework, assemblyName |
| Namespace | C# namespace | name, fullName |
| Class | Class type | name, isAbstract, isSealed, isStatic |
| Struct | Struct type | name, isReadOnly, isRefStruct |
| Interface | Interface type | name, typeParameters |
| Enum | Enum type | name, underlyingType |
| Record | Record type | name, isRecordStruct |
| Method | Method/constructor | name, returnType, isAsync, isOverride |
| Property | Property | name, type, hasGetter, hasSetter |
| Field | Field | name, type, isConst, isReadOnly |
| Event | Event | name, handlerType |
| File | Source file | path, hash, size |
| Assembly | Compiled assembly | name, version, culture |
| Package | NuGet package | name, version |

### 1.2 Edge Types

| Edge Type | Source → Target | Description |
|-----------|-----------------|-------------|
| CONTAINS | Repository → Project | Repository contains project |
| CONTAINS | Project → File | Project contains file |
| CONTAINS | Namespace → Type | Namespace contains type |
| CONTAINS | Type → Method | Type declares method |
| CONTAINS | Type → Property | Type declares property |
| CONTAINS | Type → Field | Type declares field |
| CONTAINS | Type → Event | Type declares event |
| CONTAINS | Type → NestedType | Type contains nested type |
| INHERITS | Class → Class | Class inheritance |
| IMPLEMENTS | Class → Interface | Interface implementation |
| IMPLEMENTS | Struct → Interface | Struct interface implementation |
| OVERRIDES | Method → Method | Method override |
| IMPLICITLY_IMPLEMENTS | Method → Method | Implicit interface implementation |
| CALLS | Method → Method | Method invocation |
| READS | Method → Field | Field read access |
| WRITES | Method → Field | Field write access |
| USES | Method → Type | Type usage (parameter, return, etc.) |
| USES | Method → Property | Property access |
| REFERENCES | Any → Any | General reference |
| DEPENDS_ON | Project → Project | Project dependency |
| DEPENDS_ON | Project → Package | NuGet dependency |
| DEPENDS_ON | Package → Package | Transitive dependency |
| DECLARES | File → Type | File declares type |
| DECLARES | File → Method | File declares method (top-level) |
| ATTRIBUTE | Any → Type | Attribute usage |
| RETURNS | Method → Type | Method return type |
| PARAMETER | Method → Type | Method parameter type |
| FIELD_TYPE | Field → Type | Field type |
| PROPERTY_TYPE | Property → Type | Property type |
| EVENT_HANDLER | Event → Type | Event handler type |

---

## 2. Graph Storage

### 2.1 In-Memory Graph

```csharp
class InMemoryGraph
{
    // Primary storage
    ConcurrentDictionary<GraphNodeId, GraphNode> _nodes;
    ConcurrentDictionary<GraphEdgeId, GraphEdge> _edges;
    
    // Adjacency lists for fast traversal
    ConcurrentDictionary<GraphNodeId, List<GraphEdgeId>> _outgoingEdges;
    ConcurrentDictionary<GraphNodeId, List<GraphEdgeId>> _incomingEdges;
    
    // Indexes for fast lookup
    ConcurrentDictionary<string, GraphNodeId> _nodesByFullName;
    ConcurrentDictionary<string, List<GraphNodeId>> _nodesByKind;
    ConcurrentDictionary<GraphNodeId, List<GraphNodeId>> _nodesByFile;
    
    // Type hierarchy
    Dictionary<GraphNodeId, GraphNodeId> _baseTypes;
    Dictionary<GraphNodeId, List<GraphNodeId>> _interfaceMap;
    Dictionary<GraphNodeId, List<GraphNodeId>> _derivedTypes;
}

struct GraphNodeId
{
    // SHA256 hash of fully qualified name
    byte[] Hash;
    
    // For display and collision handling
    string FullName;
    
    NodeKind Kind;
}

class GraphNode
{
    GraphNodeId Id;
    string Label;
    NodeKind Kind;
    string FullName;
    Dictionary<string, string> Metadata;
    DateTimeOffset CreatedAt;
    DateTimeOffset UpdatedAt;
}

class GraphEdge
{
    GraphEdgeId Id;
    GraphNodeId SourceId;
    GraphNodeId TargetId;
    EdgeKind Kind;
    Dictionary<string, string> Metadata;
    double Weight; // for relevance scoring
}

struct GraphEdgeId
{
    // Composite key: SourceId + TargetId + EdgeKind
    GraphNodeId SourceId;
    GraphNodeId TargetId;
    EdgeKind Kind;
}
```

### 2.2 Persistence Layer

```sql
-- PostgreSQL Schema

CREATE TABLE graph_nodes (
    id BYTEA PRIMARY KEY,           -- SHA256 hash
    full_name TEXT NOT NULL,
    label TEXT NOT NULL,
    kind VARCHAR(50) NOT NULL,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_nodes_kind ON graph_nodes(kind);
CREATE INDEX idx_nodes_name ON graph_nodes USING gin(full_name gin_trgm_ops);
CREATE INDEX idx_nodes_metadata ON graph_nodes USING gin(metadata);

CREATE TABLE graph_edges (
    id BYTEA PRIMARY KEY,
    source_id BYTEA NOT NULL REFERENCES graph_nodes(id) ON DELETE CASCADE,
    target_id BYTEA NOT NULL REFERENCES graph_nodes(id) ON DELETE CASCADE,
    kind VARCHAR(50) NOT NULL,
    weight DOUBLE PRECISION DEFAULT 1.0,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE(source_id, target_id, kind)
);

CREATE INDEX idx_edges_source ON graph_edges(source_id);
CREATE INDEX idx_edges_target ON graph_edges(target_id);
CREATE INDEX idx_edges_kind ON graph_edges(kind);
CREATE INDEX idx_edges_source_kind ON graph_edges(source_id, kind);
CREATE INDEX idx_edges_target_kind ON graph_edges(target_id, kind);

-- Materialized view for fast traversal
CREATE MATERIALIZED VIEW graph_adjacency AS
SELECT 
    source_id,
    target_id,
    kind,
    array_agg(DISTINCT kind) OVER (PARTITION BY source_id) as edge_types
FROM graph_edges;

CREATE INDEX idx_adjacency_source ON graph_adjacency(source_id);
```

---

## 3. Graph Traversal

### 3.1 BFS (Breadth-First Search)

```
class GraphBFS
{
    graph: InMemoryGraph
    
    Traverse(startNodeId: GraphNodeId, maxDepth: int, edgeFilter: EdgeKind[]):
        visited = new HashSet<GraphNodeId>()
        queue = new Queue<(GraphNodeId, int)>() // node, depth
        result = new List<GraphNodeTraversal>()
        
        queue.Enqueue((startNodeId, 0))
        visited.Add(startNodeId)
        
        while queue.Count > 0:
            (currentId, depth) = queue.Dequeue()
            
            if depth > maxDepth:
                continue
            
            // Get outgoing edges
            edges = graph.GetOutgoingEdges(currentId)
            
            // Filter by edge kind
            if edgeFilter != null:
                edges = edges.Where(e => edgeFilter.Contains(e.Kind))
            
            foreach edge in edges:
                if !visited.Contains(edge.TargetId):
                    visited.Add(edge.TargetId)
                    queue.Enqueue((edge.TargetId, depth + 1))
                    
                    result.Add(new GraphNodeTraversal {
                        NodeId = edge.TargetId,
                        Depth: depth + 1,
                        EdgeKind: edge.Kind,
                        Path: BuildPath(startNodeId, edge.TargetId)
                    })
        
        return result
    
    BuildPath(start: GraphNodeId, end: GraphNodeId):
        // Reconstruct path from BFS
        path = new List<GraphEdge>()
        current = end
        while current != start:
            // Find parent edge
            incomingEdges = graph.GetIncomingEdges(current)
            parentEdge = incomingEdges.First(e => visited.Contains(e.SourceId))
            path.Insert(0, parentEdge)
            current = parentEdge.SourceId
        return path
}
```

### 3.2 DFS (Depth-First Search)

```
class GraphDFS
{
    graph: InMemoryGraph
    
    Traverse(startNodeId: GraphNodeId, maxDepth: int, edgeFilter: EdgeKind[]):
        visited = new HashSet<GraphNodeId>()
        result = new List<GraphNodeTraversal>()
        
        DFSVisit(startNodeId, 0, maxDepth, edgeFilter, visited, result, new List<GraphEdge>())
        
        return result
    
    DFSVisit(nodeId: GraphNodeId, depth: int, maxDepth: int, 
             edgeFilter: EdgeKind[], visited: HashSet, result: List, path: List):
        
        if depth > maxDepth:
            return
        
        edges = graph.GetOutgoingEdges(nodeId)
        
        if edgeFilter != null:
            edges = edges.Where(e => edgeFilter.Contains(e.Kind))
        
        foreach edge in edges:
            if !visited.Contains(edge.TargetId):
                visited.Add(edge.TargetId)
                path.Add(edge)
                
                result.Add(new GraphNodeTraversal {
                    NodeId: edge.TargetId,
                    Depth: depth + 1,
                    EdgeKind: edge.Kind,
                    Path: new List<GraphEdge>(path)
                })
                
                DFSVisit(edge.TargetId, depth + 1, maxDepth, edgeFilter, visited, result, path)
                
                path.RemoveAt(path.Count - 1) // backtrack
}
```

### 3.3 Path Finding

```
class GraphPathFinder
{
    graph: InMemoryGraph
    
    FindPath(source: GraphNodeId, target: GraphNodeId, maxDepth: int):
        // BFS for shortest path
        visited = new Dictionary<GraphNodeId, GraphEdge>() // node → parent edge
        queue = new Queue<(GraphNodeId, int)>()
        
        queue.Enqueue((source, 0))
        visited[source] = null
        
        while queue.Count > 0:
            (currentId, depth) = queue.Dequeue()
            
            if currentId == target:
                // Reconstruct path
                return ReconstructPath(visited, source, target)
            
            if depth >= maxDepth:
                continue
            
            edges = graph.GetOutgoingEdges(currentId)
            
            foreach edge in edges:
                if !visited.ContainsKey(edge.TargetId):
                    visited[edge.TargetId] = edge
                    queue.Enqueue((edge.TargetId, depth + 1))
        
        return null // no path found
    
    ReconstructPath(visited: Dictionary, source: GraphNodeId, target: GraphNodeId):
        path = new List<GraphEdge>()
        current = target
        
        while current != source:
            edge = visited[current]
            path.Insert(0, edge)
            current = edge.SourceId
        
        return path
    
    FindAllPaths(source: GraphNodeId, target: GraphNodeId, maxDepth: int, maxPaths: int):
        paths = new List<List<GraphEdge>>()
        FindPathsRecursive(source, target, maxDepth, new HashSet(), new List(), paths, maxPaths)
        return paths
    
    FindPathsRecursive(current: GraphNodeId, target: GraphNodeId, depth: int,
                       visited: HashSet, path: List, paths: List, maxPaths: int):
        if paths.Count >= maxPaths:
            return
        
        if current == target:
            paths.Add(new List<GraphEdge>(path))
            return
        
        if depth <= 0:
            return
        
        visited.Add(current)
        edges = graph.GetOutgoingEdges(current)
        
        foreach edge in edges:
            if !visited.Contains(edge.TargetId):
                path.Add(edge)
                FindPathsRecursive(edge.TargetId, target, depth - 1, visited, path, paths, maxPaths)
                path.RemoveAt(path.Count - 1)
        
        visited.Remove(current)
}
```

### 3.4 Subgraph Extraction

```
class SubgraphExtractor
{
    graph: InMemoryGraph
    
    ExtractSubgraph(centerNodeId: GraphNodeId, depth: int, edgeFilter: EdgeKind[]):
        // BFS from center node
        traversal = graphBFS.Traverse(centerNodeId, depth, edgeFilter)
        
        // Build subgraph
        subgraph = new Subgraph()
        subgraph.CenterNodeId = centerNodeId
        
        // Add center node
        subgraph.AddNode(graph.GetNode(centerNodeId))
        
        // Add traversed nodes and edges
        foreach item in traversal:
            node = graph.GetNode(item.NodeId)
            subgraph.AddNode(node)
            
            // Find the edge that led to this node
            edge = FindEdgeToNode(centerNodeId, item.NodeId, edgeFilter)
            if edge != null:
                subgraph.AddEdge(edge)
        
        return subgraph
    
    ExtractTypeHierarchy(typeNodeId: GraphNodeId):
        subgraph = new Subgraph()
        
        // Get base types (ancestors)
        ancestors = GetAllAncestors(typeNodeId)
        foreach ancestor in ancestors:
            subgraph.AddNode(graph.GetNode(ancestor))
            subgraph.AddEdge(new GraphEdge {
                SourceId: ancestor,
                TargetId: typeNodeId, // or intermediate
                Kind: EdgeKind.INHERITS
            })
        
        // Get derived types (descendants)
        descendants = GetAllDescendants(typeNodeId)
        foreach descendant in descendants:
            subgraph.AddNode(graph.GetNode(descendant))
            subgraph.AddEdge(new GraphEdge {
                SourceId: typeNodeId, // or intermediate
                TargetId: descendant,
                Kind: EdgeKind.INHERITS
            })
        
        // Get implemented interfaces
        interfaces = GetImplementedInterfaces(typeNodeId)
        foreach iface in interfaces:
            subgraph.AddNode(graph.GetNode(iface))
            subgraph.AddEdge(new GraphEdge {
                SourceId: typeNodeId,
                TargetId: iface,
                Kind: EdgeKind.IMPLEMENTS
            })
        
        return subgraph
    
    ExtractCallGraph(methodNodeId: GraphNodeId, depth: int):
        // BFS following CALLS edges
        return ExtractSubgraph(methodNodeId, depth, new[] { EdgeKind.CALLS })
    
    ExtractDependencyGraph(projectNodeId: GraphNodeId, depth: int):
        // BFS following DEPENDS_ON edges
        return ExtractSubgraph(projectNodeId, depth, new[] { EdgeKind.DEPENDS_ON })
    
    ExtractNamespaceGraph(namespaceNodeId: GraphNodeId):
        subgraph = new Subgraph()
        
        // Get all types in namespace
        types = graph.GetNodesInNamespace(namespaceNodeId)
        foreach type in types:
            subgraph.AddNode(graph.GetNode(type))
            subgraph.AddEdge(new GraphEdge {
                SourceId: namespaceNodeId,
                TargetId: type,
                Kind: EdgeKind.CONTAINS
            })
        
        return subgraph
}
```

---

## 4. Graph Query Engine

### 4.1 Pattern Matching

```
class GraphPatternMatcher
{
    graph: InMemoryGraph
    
    FindPattern(pattern: GraphPattern):
        // Pattern: a directed graph template to match
        // Returns: all subgraphs matching the pattern
        
        results = new List<Subgraph>()
        
        // Start from pattern's root node
        patternRoot = pattern.GetRootNode()
        
        // Find all candidate nodes
        candidates = graph.GetNodesByKind(patternRoot.Kind)
        
        foreach candidate in candidates:
            if MatchPattern(pattern, patternRoot, candidate, new Dictionary()):
                // Found a match
                subgraph = ExtractMatch(pattern, candidate)
                results.Add(subgraph)
        
        return results
    
    MatchPattern(pattern: GraphPattern, patternNode: PatternNode, 
                 graphNode: GraphNodeId, bindings: Dictionary):
        // Check node kind
        if patternNode.Kind != graph.GetNode(graphNode).Kind:
            return false
        
        // Check constraints
        foreach constraint in patternNode.Constraints:
            if !EvaluateConstraint(constraint, graphNode):
                return false
        
        // Bind variable if present
        if patternNode.Variable != null:
            bindings[patternNode.Variable] = graphNode
        
        // Check edges
        foreach patternEdge in patternNode.OutgoingEdges:
            graphEdges = graph.GetOutgoingEdges(graphNode)
                .Where(e => e.Kind == patternEdge.Kind)
            
            if patternEdge.TargetNode != null:
                // Must match specific target
                found = false
                foreach graphEdge in graphEdges:
                    if MatchPattern(pattern, patternEdge.TargetNode, graphEdge.TargetId, bindings):
                        found = true
                        break
                
                if !found:
                    return false
            else:
                // Any target is acceptable
                if graphEdges.Count == 0:
                    return false
        
        return true
    
    // Example patterns:
    
    // Find all methods that call Weapon.Fire()
    FindWeaponFireCallers():
        pattern = GraphPattern()
            .Node("caller", NodeKind.Method)
            .Edge("caller", EdgeKind.CALLS, "fireMethod")
            .Node("fireMethod", NodeKind.Method, 
                   Constraint("fullName", "equals", "Weapon.Fire"))
        return FindPattern(pattern)
    
    // Find all classes implementing IDisposable
    FindDisposableImplementors():
        pattern = GraphPattern()
            .Node("implementor", NodeKind.Class)
            .Edge("implementor", EdgeKind.IMPLEMENTS, "iface")
            .Node("iface", NodeKind.Interface,
                   Constraint("fullName", "equals", "System.IDisposable"))
        return FindPattern(pattern)
    
    // Find all Unity MonoBehaviours with Update methods
    FindMonoBehavioursWithUpdate():
        pattern = GraphPattern()
            .Node("mb", NodeKind.Class, 
                   Constraint("metadata.IsMonoBehaviour", "equals", "true"))
            .Edge("mb", EdgeKind.CONTAINS, "updateMethod")
            .Node("updateMethod", NodeKind.Method,
                   Constraint("name", "equals", "Update"))
        return FindPattern(pattern)
}
```

### 4.2 Graph Scoring

```
class GraphScorer
{
    graph: InMemoryGraph
    
    // Score nodes based on centrality
    CalculatePageRank(iterations: int, dampingFactor: double):
        scores = new Dictionary<GraphNodeId, double>()
        
        // Initialize all scores to 1/N
        allNodes = graph.GetAllNodes()
        initialScore = 1.0 / allNodes.Count
        
        foreach node in allNodes:
            scores[node.Id] = initialScore
        
        // Iterate
        for i in 0 to iterations:
            newScores = new Dictionary<GraphNodeId, double>()
            
            foreach node in allNodes:
                // Sum contributions from incoming edges
                incomingEdges = graph.GetIncomingEdges(node.Id)
                incomingScore = 0.0
                
                foreach edge in incomingEdges:
                    sourceNode = graph.GetNode(edge.SourceId)
                    outgoingCount = graph.GetOutgoingEdges(edge.SourceId).Count
                    incomingScore += scores[edge.SourceId] / outgoingCount
                
                // Apply damping factor
                newScores[node.Id] = (1 - dampingFactor) / allNodes.Count + dampingFactor * incomingScore
            
            scores = newScores
        
        return scores
    
    // Score based on graph distance from query
    CalculateRelevanceScores(queryNodeIds: List<GraphNodeId>, maxDepth: int):
        scores = new Dictionary<GraphNodeId, double>()
        
        foreach queryNodeId in queryNodeIds:
            // BFS from query node
            traversal = graphBFS.Traverse(queryNodeId, maxDepth, null)
            
            foreach item in traversal:
                // Score decreases with distance
                distanceScore = 1.0 / (1.0 + item.Depth)
                
                // Accumulate scores from multiple query nodes
                if scores.ContainsKey(item.NodeId):
                    scores[item.NodeId] += distanceScore
                else:
                    scores[item.NodeId] = distanceScore
        
        return scores
    
    // Score based on reference count
    CalculateReferenceScores():
        scores = new Dictionary<GraphNodeId, double>()
        
        foreach node in graph.GetAllNodes():
            incomingCount = graph.GetIncomingEdges(node.Id).Count
            // Logarithmic scaling
            scores[node.Id] = Math.Log(1 + incomingCount)
        
        return scores
    
    // Combined relevance score
    CalculateCombinedScore(queryNodeIds: List<GraphNodeId>, weights: ScoreWeights):
        pageRankScores = CalculatePageRank(10, 0.85)
        distanceScores = CalculateRelevanceScores(queryNodeIds, 5)
        referenceScores = CalculateReferenceScores()
        
        combinedScores = new Dictionary<GraphNodeId, double>()
        
        foreach node in graph.GetAllNodes():
            combinedScores[node.Id] = 
                weights.PageRank * pageRankScores.GetValueOrDefault(node.Id, 0) +
                weights.Distance * distanceScores.GetValueOrDefault(node.Id, 0) +
                weights.Reference * referenceScores.GetValueOrDefault(node.Id, 0)
        
        return combinedScores
}

struct ScoreWeights
{
    double PageRank;
    double Distance;
    double Reference;
}
```

---

## 5. Graph Queries

### 5.1 Find All Callers

```
FindCallers(methodNodeId: GraphNodeId, maxDepth: int):
    // Find all methods that call this method
    incomingEdges = graph.GetIncomingEdges(methodNodeId)
        .Where(e => e.Kind == EdgeKind.CALLS)
    
    callers = new List<GraphNodeId>()
    
    foreach edge in incomingEdges:
        callers.Add(edge.SourceId)
        
        // Optionally traverse deeper
        if maxDepth > 1:
            indirectCallers = FindCallers(edge.SourceId, maxDepth - 1)
            callers.AddRange(indirectCallers)
    
    return callers.Distinct()
```

### 5.2 Find All Callees

```
FindCallees(methodNodeId: GraphNodeId, maxDepth: int):
    // Find all methods called by this method
    outgoingEdges = graph.GetOutgoingEdges(methodNodeId)
        .Where(e => e.Kind == EdgeKind.CALLS)
    
    callees = new List<GraphNodeId>()
    
    foreach edge in outgoingEdges:
        callees.Add(edge.TargetId)
        
        // Optionally traverse deeper
        if maxDepth > 1:
            indirectCallees = FindCallees(edge.TargetId, maxDepth - 1)
            callees.AddRange(indirectCallees)
    
    return callees.Distinct()
```

### 5.3 Find Implementations

```
FindImplementations(interfaceNodeId: GraphNodeId):
    // Find all types implementing this interface
    incomingEdges = graph.GetIncomingEdges(interfaceNodeId)
        .Where(e => e.Kind == EdgeKind.IMPLEMENTS)
    
    implementors = incomingEdges.Select(e => e.SourceId).ToList()
    
    // Also find implementations of derived interfaces
    derivedInterfaces = GetDerivedInterfaces(interfaceNodeId)
    foreach derived in derivedInterfaces:
        derivedImplementors = FindImplementations(derived)
        implementors.AddRange(derivedImplementors)
    
    return implementors.Distinct()
```

### 5.4 Find Derived Types

```
FindDerivedTypes(typeNodeId: GraphNodeId):
    // Find all types inheriting from this type
    incomingEdges = graph.GetIncomingEdges(typeNodeId)
        .Where(e => e.Kind == EdgeKind.INHERITS)
    
    derived = incomingEdges.Select(e => e.SourceId).ToList()
    
    // Recursively find derived types
    foreach derivedType in derived.ToList():
        indirectDerived = FindDerivedTypes(derivedType)
        derived.AddRange(indirectDerived)
    
    return derived.Distinct()
```

### 5.5 Find Dependencies

```
FindDependencies(projectNodeId: GraphNodeId, depth: int):
    // Find all transitive dependencies
    dependencies = new HashSet<GraphNodeId>()
    
    BFS(projectNodeId, depth, new[] { EdgeKind.DEPENDS_ON }, dependencies)
    
    return dependencies
```

---

## 6. Graph Update Operations

### 6.1 Add Node

```
AddNode(node: GraphNode):
    // Check for existing node with same full name
    existingId = _nodesByFullName.GetValueOrDefault(node.FullName)
    
    if existingId != null:
        // Update existing node
        UpdateNode(existingId, node)
        return existingId
    
    // Add new node
    _nodes[node.Id] = node
    _nodesByFullName[node.FullName] = node.Id
    
    // Update kind index
    if !_nodesByKind.ContainsKey(node.Kind):
        _nodesByKind[node.Kind] = new List<GraphNodeId>()
    _nodesByKind[node.Kind].Add(node.Id)
    
    return node.Id
```

### 6.2 Add Edge

```
AddEdge(edge: GraphEdge):
    // Check for existing edge
    edgeId = new GraphEdgeId(edge.SourceId, edge.TargetId, edge.Kind)
    
    if _edges.ContainsKey(edgeId):
        // Update existing edge
        _edges[edgeId] = edge
        return edgeId
    
    // Add new edge
    _edges[edgeId] = edge
    
    // Update adjacency lists
    if !_outgoingEdges.ContainsKey(edge.SourceId):
        _outgoingEdges[edge.SourceId] = new List<GraphEdgeId>()
    _outgoingEdges[edge.SourceId].Add(edgeId)
    
    if !_incomingEdges.ContainsKey(edge.TargetId):
        _incomingEdges[edge.TargetId] = new List<GraphEdgeId>()
    _incomingEdges[edge.TargetId].Add(edgeId)
    
    // Update type hierarchy if applicable
    if edge.Kind == EdgeKind.INHERITS:
        _baseTypes[edge.SourceId] = edge.TargetId
        
        if !_derivedTypes.ContainsKey(edge.TargetId):
            _derivedTypes[edge.TargetId] = new List<GraphNodeId>()
        _derivedTypes[edge.TargetId].Add(edge.SourceId)
    
    if edge.Kind == EdgeKind.IMPLEMENTS:
        if !_interfaceMap.ContainsKey(edge.SourceId):
            _interfaceMap[edge.SourceId] = new List<GraphNodeId>()
        _interfaceMap[edge.SourceId].Add(edge.TargetId)
    
    return edgeId
```

### 6.3 Remove Node

```
RemoveNode(nodeId: GraphNodeId):
    // Remove all edges connected to this node
    outgoing = _outgoingEdges.GetValueOrDefault(nodeId, new List<GraphEdgeId>())
    incoming = _incomingEdges.GetValueOrDefault(nodeId, new List<GraphEdgeId>())
    
    foreach edgeId in outgoing:
        RemoveEdge(edgeId)
    
    foreach edgeId in incoming:
        RemoveEdge(edgeId)
    
    // Remove node
    _nodes.TryRemove(nodeId, out _)
    
    // Remove from indexes
    node = // get before removal
    _nodesByFullName.Remove(node.FullName)
    _nodesByKind[node.Kind].Remove(nodeId)
    
    // Remove from type hierarchy
    _baseTypes.Remove(nodeId)
    _interfaceMap.Remove(nodeId)
    _derivedTypes.Remove(nodeId)
    
    // Remove from derived types lists
    foreach kvp in _derivedTypes:
        kvp.Value.Remove(nodeId)
```

### 6.4 Bulk Operations

```
class BulkGraphUpdater
{
    graph: InMemoryGraph
    pendingNodes: List<GraphNode>
    pendingEdges: List<GraphEdge>
    
    BeginBatch():
        pendingNodes = new List<GraphNode>()
        pendingEdges = new List<GraphEdge>()
    
    QueueNode(node: GraphNode):
        pendingNodes.Add(node)
    
    QueueEdge(edge: GraphEdge):
        pendingEdges.Add(edge)
    
    CommitBatch():
        // Process nodes first
        foreach node in pendingNodes:
            graph.AddNode(node)
        
        // Then edges (nodes must exist)
        foreach edge in pendingEdges:
            graph.AddEdge(edge)
        
        // Persist to database
        PersistBatch(pendingNodes, pendingEdges)
        
        // Clear pending
        pendingNodes.Clear()
        pendingEdges.Clear()
    
    PersistBatch(nodes: List<GraphNode>, edges: List<GraphEdge>):
        // Bulk insert to PostgreSQL
        using var connection = new NpgsqlConnection(connectionString)
        connection.Open()
        
        // Insert nodes
        using var nodeCommand = connection.CreateCommand()
        nodeCommand.CommandText = @"
            INSERT INTO graph_nodes (id, full_name, label, kind, metadata)
            VALUES (@id, @fullName, @label, @kind, @metadata::jsonb)
            ON CONFLICT (id) DO UPDATE SET
                label = EXCLUDED.label,
                metadata = EXCLUDED.metadata,
                updated_at = NOW()
            "
        
        foreach node in nodes:
            nodeCommand.Parameters.Clear()
            nodeCommand.Parameters.AddWithValue("@id", node.Id.Hash)
            nodeCommand.Parameters.AddWithValue("@fullName", node.FullName)
            nodeCommand.Parameters.AddWithValue("@label", node.Label)
            nodeCommand.Parameters.AddWithValue("@kind", node.Kind.ToString())
            nodeCommand.Parameters.AddWithValue("@metadata", JsonSerializer.Serialize(node.Metadata))
            nodeCommand.ExecuteNonQuery()
        
        // Insert edges
        using var edgeCommand = connection.CreateCommand()
        edgeCommand.CommandText = @"
            INSERT INTO graph_edges (id, source_id, target_id, kind, weight, metadata)
            VALUES (@id, @sourceId, @targetId, @kind, @weight, @metadata::jsonb)
            ON CONFLICT (source_id, target_id, kind) DO UPDATE SET
                weight = EXCLUDED.weight,
                metadata = EXCLUDED.metadata
            "
        
        foreach edge in edges:
            edgeCommand.Parameters.Clear()
            edgeCommand.Parameters.AddWithValue("@id", edge.Id.ToBytes())
            edgeCommand.Parameters.AddWithValue("@sourceId", edge.SourceId.Hash)
            edgeCommand.Parameters.AddWithValue("@targetId", edge.TargetId.Hash)
            edgeCommand.Parameters.AddWithValue("@kind", edge.Kind.ToString())
            edgeCommand.Parameters.AddWithValue("@weight", edge.Weight)
            edgeCommand.Parameters.AddWithValue("@metadata", JsonSerializer.Serialize(edge.Metadata))
            edgeCommand.ExecuteNonQuery()
}
```

---

## 7. Example: Unity Weapon System Graph

```
Graph Structure:

PlayerController (Class)
  └──DECLARES──→ Attack() (Method)
                   └──CALLS──→ Weapon.Fire() (Method)
                                 └──CALLS──→ Projectile.Spawn() (Method)
                                               └──CALLS──→ DamageSystem.ApplyDamage() (Method)
                                                             └──USES──→ DamageData (Struct)

Weapon (Class)
  └──DECLARES──→ Fire() (Method)
  └──DECLARES──→ Ammo (Field)
  └──DECLARES──→ FireRate (Property)

Projectile (Class)
  └──DECLARES──→ Spawn() (Method)
  └──DECLARES──→ Damage (Field)
  └──DECLARES──→ Speed (Field)

DamageSystem (Class)
  └──DECLARES──→ ApplyDamage() (Method)
  └──IMPLEMENTS──→ IDamageHandler (Interface)

DamageData (Struct)
  └──DECLARES──→ Amount (Field)
  └──DECLARES──→ Source (Field)

IDamageHandler (Interface)
  └──DECLARES──→ HandleDamage() (Method)
```

### Query Example: "How does weapon firing work?"

```
1. Find "Weapon" node
   → GraphNode { Name: "Weapon", Kind: Class }

2. Find "Fire" method in Weapon
   → GraphNode { Name: "Fire", Kind: Method }
   → Edge: Weapon --DECLARES--> Fire

3. Find callees of Fire
   → Fire --CALLS--> Projectile.Spawn
   → Fire --CALLS--> DamageSystem.ApplyDamage

4. Find callees of Spawn
   → Spawn --CALLS--> (no further calls)

5. Find callees of ApplyDamage
   → ApplyDamage --USES--> DamageData

6. Build context from all found nodes
   → Weapon, Fire, Projectile, Spawn, DamageSystem, ApplyDamage, DamageData
```
