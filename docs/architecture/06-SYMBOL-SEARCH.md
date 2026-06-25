# Nexus Code Intelligence Platform - Symbol Search Engine

## Overview

The Symbol Search Engine provides fast, graph-based code navigation without embeddings. It uses the Knowledge Graph and Symbol Table built by the Roslyn Analysis Engine.

---

## 1. Architecture

```
┌─────────────────────────────────────────────────────┐
│              Symbol Search Engine                     │
├─────────────────────────────────────────────────────┤
│                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────┐ │
│  │ Symbol Index  │  │ Graph        │  │ Reference │ │
│  │ (by name)     │  │ Traversal    │  │ Tracker   │ │
│  └──────────────┘  └──────────────┘  └───────────┘ │
│         ↓                  ↓                ↓        │
│  ┌──────────────────────────────────────────────┐   │
│  │           Query Executor                      │   │
│  └──────────────────────────────────────────────┘   │
│                                                      │
└─────────────────────────────────────────────────────┘
```

---

## 2. Core Data Structures

```csharp
class SymbolSearchIndex
{
    // Primary: name → symbols (case-insensitive)
    ConcurrentDictionary<string, List<SymbolId>> _nameIndex;
    
    // Full name index: "Namespace.Type.Method" → symbol
    ConcurrentDictionary<string, SymbolId> _fullNameIndex;
    
    // Fuzzy index: trigrams of names
    ConcurrentDictionary<string, List<SymbolId>> _trigramIndex;
    
    // Kind index: SymbolKind → symbols
    ConcurrentDictionary<SymbolKind, List<SymbolId>> _kindIndex;
    
    // File index: file path → symbols in file
    ConcurrentDictionary<string, List<SymbolId>> _fileIndex;
    
    // Project index: project → symbols
    ConcurrentDictionary<ProjectId, List<SymbolId>> _projectIndex;
}
```

---

## 3. Search Operations

### 3.1 Find Symbol by Name

```
FindSymbol(query: string, options: SearchOptions):
    results = new List<SearchResult>()
    
    // Exact match on full name
    exactMatch = _fullNameIndex.GetValueOrDefault(query)
    if exactMatch != null:
        results.Add(new SearchResult {
            Symbol: GetSymbolInfo(exactMatch),
            Score: 1.0,
            MatchType = MatchType.Exact
        })
    
    // Exact match on simple name
    nameMatches = _nameIndex.GetValueOrDefault(query)
    if nameMatches != null:
        foreach match in nameMatches:
            results.Add(new SearchResult {
                Symbol: GetSymbolInfo(match),
                Score = 0.9,
                MatchType = MatchType.Name
            })
    
    // Prefix match
    prefixMatches = FindByPrefix(query)
    results.AddRange(prefixMatches)
    
    // Fuzzy match using trigrams
    if results.Count < options.MinResults:
        fuzzyMatches = FindByFuzzy(query)
        results.AddRange(fuzzyMatches)
    
    // Filter by kind if specified
    if options.KindFilter != null:
        results = results.Where(r => r.Symbol.Kind == options.KindFilter)
    
    // Sort by relevance
    return results.OrderByDescending(r => r.Score).Take(options.MaxResults)

FindByPrefix(prefix: string):
    // Use sorted index for prefix search
    results = new List<SearchResult>()
    
    foreach kvp in _nameIndex:
        if kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase):
            foreach symbolId in kvp.Value:
                results.Add(new SearchResult {
                    Symbol = GetSymbolInfo(symbolId),
                    Score = 0.7,
                    MatchType = MatchType.Prefix
                })
    
    return results

FindByFuzzy(query: string):
    // Generate trigrams from query
    queryTrigrams = GenerateTrigrams(query)
    
    // Find symbols with matching trigrams
    candidateScores = new Dictionary<SymbolId, int>()
    
    foreach trigram in queryTrigrams:
        matches = _trigramIndex.GetValueOrDefault(trigram)
        if matches != null:
            foreach match in matches:
                if candidateScores.ContainsKey(match):
                    candidateScores[match]++
                else:
                    candidateScores[match] = 1
    
    // Rank by trigram overlap
    return candidateScores
        .OrderByDescending(kvp => kvp.Value)
        .Take(20)
        .Select(kvp => new SearchResult {
            Symbol = GetSymbolInfo(kvp.Key),
            Score = 0.3 + (kvp.Value / (double)queryTrigrams.Length) * 0.4,
            MatchType = MatchType.Fuzzy
        })
```

### 3.2 Find References

```
FindReferences(symbolId: SymbolId, options: ReferenceOptions):
    // Get all references to this symbol
    references = _referenceTracker.GetReferences(symbolId)
    
    results = new List<ReferenceResult>()
    
    foreach reference in references:
        // Skip if in definition file and option is set
        if options.ExcludeDefinition && reference.IsDefinition:
            continue
        
        // Skip if in different project and option is set
        if options.ProjectFilter != null && reference.ProjectId != options.ProjectFilter:
            continue
        
        // Get context snippet
        snippet = GetCodeSnippet(reference.FilePath, reference.Line, options.ContextLines)
        
        results.Add(new ReferenceResult {
            Reference = reference,
            Snippet = snippet,
            Score = CalculateReferenceScore(reference, options)
        })
    
    return results.OrderByDescending(r => r.Score)

CalculateReferenceScore(reference: ReferenceInfo, options: ReferenceOptions):
    score = 1.0
    
    // Boost direct references
    if reference.Kind == ReferenceKind.Read:
        score *= 1.0
    elif reference.Kind == ReferenceKind.Write:
        score *= 0.9
    elif reference.Kind == ReferenceKind.Call:
        score *= 0.95
    
    // Boost same-file references
    if reference.FilePath == options.SourceFilePath:
        score *= 1.2
    
    // Boost same-project references
    if reference.ProjectId == options.SourceProjectId:
        score *= 1.1
    
    return score
```

### 3.3 Find Callers

```
FindCallers(methodId: SymbolId, options: CallerOptions):
    // Find all methods that call this method
    incomingEdges = graph.GetIncomingEdges(methodId)
        .Where(e => e.Kind == EdgeKind.CALLS)
    
    callers = new List<CallerResult>()
    
    foreach edge in incomingEdges:
        callerSymbol = GetSymbolInfo(edge.SourceId)
        
        // Get the call site context
        callSite = GetCallSiteContext(edge)
        
        callers.Add(new CallerResult {
            Method = callerSymbol,
            CallSite = callSite,
            Score = CalculateCallerScore(callerSymbol, options)
        })
        
        // Optionally find indirect callers
        if options.MaxDepth > 1:
            indirectCallers = FindCallers(edge.SourceId, new CallerOptions {
                MaxDepth = options.MaxDepth - 1
            })
            callers.AddRange(indirectCallers)
    
    return callers.DistinctBy(c => c.Method.Id).OrderByDescending(c => c.Score)

GetCallSiteContext(edge: GraphEdge):
    // Get the reference that represents this call
    reference = _referenceTracker.GetReference(edge)
    
    if reference == null:
        return null
    
    // Get surrounding code
    return GetCodeSnippet(reference.FilePath, reference.Line, contextLines: 3)
```

### 3.4 Find Callees

```
FindCallees(methodId: SymbolId, options: CalleeOptions):
    // Find all methods called by this method
    outgoingEdges = graph.GetOutgoingEdges(methodId)
        .Where(e => e.Kind == EdgeKind.CALLS)
    
    callees = new List<CalleeResult>()
    
    foreach edge in outgoingEdges:
        calleeSymbol = GetSymbolInfo(edge.TargetId)
        
        // Get the call site context
        callSite = GetCallSiteContext(edge)
        
        callees.Add(new CalleeResult {
            Method = calleeSymbol,
            CallSite = callSite,
            Score = CalculateCalleeScore(calleeSymbol, options)
        })
        
        // Optionally find indirect callees
        if options.MaxDepth > 1:
            indirectCallees = FindCallees(edge.TargetId, new CalleeOptions {
                MaxDepth = options.MaxDepth - 1
            })
            callees.AddRange(indirectCallees)
    
    return callees.DistinctBy(c => c.Method.Id).OrderByDescending(c => c.Score)
```

### 3.5 Find Implementations

```
FindImplementations(interfaceId: SymbolId):
    // Find all types implementing this interface
    incomingEdges = graph.GetIncomingEdges(interfaceId)
        .Where(e => e.Kind == EdgeKind.IMPLEMENTS)
    
    implementors = new List<ImplementationResult>()
    
    foreach edge in incomingEdges:
        typeSymbol = GetSymbolInfo(edge.SourceId)
        
        // Find the implementing methods
        implementingMethods = GetImplementingMethods(edge.SourceId, interfaceId)
        
        implementors.Add(new ImplementationResult {
            Type = typeSymbol,
            ImplementingMethods = implementingMethods
        })
    
    // Also find implementations of derived interfaces
    derivedInterfaces = graph.GetDerivedInterfaces(interfaceId)
    foreach derived in derivedInterfaces:
        derivedImplementors = FindImplementations(derived)
        implementors.AddRange(derivedImplementors)
    
    return implementors.DistinctBy(i => i.Type.Id)

GetImplementingMethods(typeId: SymbolId, interfaceId: SymbolId):
    // Find methods in type that implement interface methods
    interfaceMethods = graph.GetOutgoingEdges(interfaceId)
        .Where(e => e.Kind == EdgeKind.DECLARES && graph.GetNode(e.TargetId).Kind == NodeKind.Method)
        .Select(e => e.TargetId)
    
    implementingMethods = new List<MethodImplementation>()
    
    foreach interfaceMethod in interfaceMethods:
        // Find the implementing method in the type
        implementingMethod = FindImplementingMethod(typeId, interfaceMethod)
        if implementingMethod != null:
            implementingMethods.Add(new MethodImplementation {
                InterfaceMethod = GetMethodInfo(interfaceMethod),
                ImplementingMethod = GetMethodInfo(implementingMethod)
            })
    
    return implementingMethods
```

### 3.6 Find Derived Types

```
FindDerivedTypes(typeId: SymbolId, recursive: bool):
    // Find all types inheriting from this type
    derivedTypeIds = graph.GetDerivedTypes(typeId)
    
    results = new List<TypeHierarchyResult>()
    
    foreach derivedId in derivedTypeIds:
        typeSymbol = GetSymbolInfo(derivedId)
        
        results.Add(new TypeHierarchyResult {
            Type = typeSymbol,
            Depth = CalculateDepth(typeId, derivedId)
        })
        
        // Recursively find derived types
        if recursive:
            indirectDerived = FindDerivedTypes(derivedId, true)
            results.AddRange(indirectDerived)
    
    return results.OrderBy(r => r.Depth)
```

### 3.7 Find Overrides

```
FindOverrides(methodId: SymbolId):
    // Find all methods that override this method
    incomingEdges = graph.GetIncomingEdges(methodId)
        .Where(e => e.Kind == EdgeKind.OVERRIDES)
    
    overrides = new List<OverrideResult>()
    
    foreach edge in incomingEdges:
        overridingMethod = GetMethodInfo(edge.SourceId)
        declaringType = GetTypeInfo(overridingMethod.DeclaringTypeId)
        
        overrides.Add(new OverrideResult {
            Method = overridingMethod,
            DeclaringType = declaringType
        })
        
        // Find further overrides
        indirectOverrides = FindOverrides(edge.SourceId)
        overrides.AddRange(indirectOverrides)
    
    return overrides
```

---

## 4. Complexity Analysis

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|-----------------|
| Find Symbol (exact) | O(1) | O(1) |
| Find Symbol (prefix) | O(P) where P = prefix matches | O(P) |
| Find Symbol (fuzzy) | O(T) where T = trigram count | O(C) where C = candidates |
| Find References | O(R) where R = reference count | O(R) |
| Find Callers (depth 1) | O(D) where D = in-degree | O(D) |
| Find Callers (depth N) | O(D^N) | O(D^N) |
| Find Callees (depth 1) | O(D) where D = out-degree | O(D) |
| Find Implementations | O(I) where I = implementors | O(I) |
| Find Derived Types | O(H) where H = hierarchy size | O(H) |
| Find Overrides | O(O) where O = override count | O(O) |

---

## 5. Search Options

```csharp
class SearchOptions
{
    int MaxResults { get; set; } = 50;
    int MinResults { get; set; } = 5;
    SymbolKind? KindFilter { get; set; }
    string? ProjectFilter { get; set; }
    string? NamespaceFilter { get; set; }
    bool IncludeGenerated { get; set; } = false;
    bool CaseSensitive { get; set; } = false;
}

class ReferenceOptions
{
    bool ExcludeDefinition { get; set; } = true;
    int ContextLines { get; set; } = 2;
    ProjectId? ProjectFilter { get; set; }
    string? SourceFilePath { get; set; }
    ProjectId? SourceProjectId { get; set; }
}

class CallerOptions
{
    int MaxDepth { get; set; } = 3;
    bool IncludeIndirect { get; set; } = true;
    SymbolKind? CallerFilter { get; set; }
}

class CalleeOptions
{
    int MaxDepth { get; set; } = 3;
    bool IncludeIndirect { get; set; } = true;
    SymbolKind? CalleeFilter { get; set; }
}
```
