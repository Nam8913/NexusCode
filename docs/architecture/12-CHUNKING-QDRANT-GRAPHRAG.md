# Nexus Code Intelligence Platform - Chunking Strategy, Qdrant Schema, Graph RAG

---

## 1. Chunking Strategy

### 1.1 Overview

The chunking strategy determines how code is split into units for embedding. We use a multi-level approach that preserves semantic boundaries.

### 1.2 Chunk Types

#### Level 1: File Chunk

```
FileChunk:
  Content: Entire file content
  Size: Variable (can be very large)
  Metadata:
    - filePath
    - projectName
    - repositoryId
    - language
    - lineCount
    - symbolCount
  
  Trade-offs:
    + Preserves full context
    + Simple to generate
    - May exceed token limits
    - Poor granularity for search
```

#### Level 2: Type Chunk

```
TypeChunk:
  Content: Complete type definition (class, struct, interface, enum)
  Size: ~100-500 lines typical
  Metadata:
    - filePath
    - typeName
    - typeKind
    - namespace
    - baseType
    - interfaces
    - memberCount
  
  Trade-offs:
    + Good semantic unit
    + Includes all members
    - May still be large for complex types
    - Cross-type references lost
```

#### Level 3: Method Chunk

```
MethodChunk:
  Content: Single method with its signature and body
  Size: ~10-100 lines typical
  Metadata:
    - filePath
    - typeName
    - methodName
    - returnType
    - parameterTypes
    - isAsync
    - isOverride
  
  Trade-offs:
    + Fine-grained search
    + Good for method-level queries
    - Loses class context
    - May miss relationships between methods
```

#### Level 4: Semantic Chunk

```
SemanticChunk:
  Content: Code block with semantic meaning (if/else, try/catch, loop)
  Size: ~5-50 lines typical
  Metadata:
    - filePath
    - typeName
    - methodName
    - blockKind (if, else, try, catch, loop, etc.)
    - startLine
    - endLine
  
  Trade-offs:
    + Very fine-grained
    + Good for specific patterns
    - Loses surrounding context
    - May be too small for meaningful embedding
```

#### Level 5: Graph Context Chunk

```
GraphContextChunk:
  Content: Code snippet with surrounding graph context
  Size: ~20-100 lines typical
  Metadata:
    - filePath
    - symbolName
    - symbolKind
    - relatedSymbols[]
    - callGraphDepth
    - dependencyDepth
  
  Trade-offs:
    + Includes relationships
    + Good for understanding code flow
    - Requires graph traversal
    - More expensive to generate
```

### 1.3 Hybrid Chunking

```
class HybridChunker
{
    GenerateChunks(sourceFile: SourceFile):
        chunks = new List<Chunk>()
        
        // Level 1: File chunk (for full-text search)
        chunks.Add(CreateFileChunk(sourceFile))
        
        // Level 2: Type chunks
        foreach type in sourceFile.Types:
            chunks.Add(CreateTypeChunk(type))
            
            // Level 3: Method chunks within type
            foreach method in type.Methods:
                chunks.Add(CreateMethodChunk(method))
                
                // Level 4: Semantic chunks within method
                foreach block in method.SemanticBlocks:
                    chunks.Add(CreateSemanticChunk(block))
        
        // Level 5: Graph context chunks (for key methods)
        foreach method in sourceFile.KeyMethods:
            chunks.Add(CreateGraphContextChunk(method))
        
        return chunks
    
    CreateFileChunk(sourceFile: SourceFile):
        return new Chunk {
            Id = Guid.NewGuid(),
            Content = sourceFile.Content,
            ChunkType = ChunkType.File,
            Metadata = new Dictionary<string, string> {
                ["filePath"] = sourceFile.FilePath,
                ["projectName"] = sourceFile.ProjectName,
                ["repositoryId"] = sourceFile.RepositoryId.ToString(),
                ["lineCount"] = sourceFile.LineCount.ToString(),
                ["symbolCount"] = sourceFile.SymbolCount.ToString()
            },
            TokenCount = EstimateTokens(sourceFile.Content)
        }
    
    CreateTypeChunk(type: TypeInfo):
        // Extract type content from syntax tree
        content = ExtractTypeContent(type)
        
        return new Chunk {
            Id = Guid.NewGuid(),
            Content = content,
            ChunkType = ChunkType.Type,
            SymbolId = type.Id,
            Metadata = new Dictionary<string, string> {
                ["filePath"] = type.FilePath,
                ["typeName"] = type.FullName,
                ["typeKind"] = type.Kind.ToString(),
                ["namespace"] = type.Namespace,
                ["baseType"] = type.BaseType ?? "",
                ["interfaces"] = string.Join(",", type.Interfaces),
                ["memberCount"] = type.MemberCount.ToString()
            },
            TokenCount = EstimateTokens(content)
        }
    
    CreateMethodChunk(method: MethodInfo):
        content = ExtractMethodContent(method)
        
        return new Chunk {
            Id = Guid.NewGuid(),
            Content = content,
            ChunkType = ChunkType.Method,
            SymbolId = method.Id,
            Metadata = new Dictionary<string, string> {
                ["filePath"] = method.FilePath,
                ["typeName"] = method.DeclaringTypeName,
                ["methodName"] = method.FullName,
                ["returnType"] = method.ReturnType,
                ["parameterTypes"] = string.Join(",", method.ParameterTypes),
                ["isAsync"] = method.IsAsync.ToString(),
                ["isOverride"] = method.IsOverride.ToString()
            },
            TokenCount = EstimateTokens(content)
        }
    
    CreateSemanticChunk(block: SemanticBlock):
        content = ExtractBlockContent(block)
        
        return new Chunk {
            Id = Guid.NewGuid(),
            Content = content,
            ChunkType = ChunkType.Semantic,
            Metadata = new Dictionary<string, string> {
                ["filePath"] = block.FilePath,
                ["typeName"] = block.TypeName,
                ["methodName"] = block.MethodName,
                ["blockKind"] = block.Kind.ToString(),
                ["startLine"] = block.StartLine.ToString(),
                ["endLine"] = block.EndLine.ToString()
            },
            TokenCount = EstimateTokens(content)
        }
    
    CreateGraphContextChunk(method: MethodInfo):
        // Get method content with surrounding context
        content = ExtractMethodContent(method)
        
        // Get related symbols from graph
        relatedSymbols = GetRelatedSymbols(method.Id, depth: 2)
        
        contextHeader = $"// Method: {method.FullName}\n"
        contextHeader += $"// Declaring Type: {method.DeclaringTypeName}\n"
        contextHeader += $"// Called by: {string.Join(", ", relatedSymbols.Callers.Take(3))}\n"
        contextHeader += $"// Calls: {string.Join(", ", relatedSymbols.Callees.Take(3))}\n"
        contextHeader += "\n"
        
        return new Chunk {
            Id = Guid.NewGuid(),
            Content = contextHeader + content,
            ChunkType = ChunkType.GraphContext,
            SymbolId = method.Id,
            Metadata = new Dictionary<string, string> {
                ["filePath"] = method.FilePath,
                ["typeName"] = method.DeclaringTypeName,
                ["methodName"] = method.FullName,
                ["relatedSymbols"] = string.Join(",", relatedSymbols.All.Select(s => s.Name)),
                ["callGraphDepth"] = "2",
                ["dependencyDepth"] = "2"
            },
            TokenCount = EstimateTokens(contextHeader + content)
        }
    
    EstimateTokens(content: string):
        // Simple heuristic: ~4 chars per token for English, ~2 for C#
        return (int)Math.Ceiling(content.Length / 3.0)
    }
```

### 1.4 Chunk Size Guidelines

| Chunk Type | Target Size | Max Size | Min Size |
|------------|-------------|----------|----------|
| File | Full file | 8192 tokens | 100 tokens |
| Type | 200-500 lines | 4096 tokens | 50 tokens |
| Method | 20-100 lines | 2048 tokens | 20 tokens |
| Semantic | 10-50 lines | 512 tokens | 10 tokens |
| Graph Context | 30-100 lines | 2048 tokens | 50 tokens |

---

## 2. Qdrant Schema

### 2.1 Collection Definition

```json
{
  "vectors": {
    "size": 768,
    "distance": "Cosine"
  },
  "optimizers_config": {
    "indexing_threshold": 20000
  },
  "payload_schema": {
    "repositoryId": {
      "type": "keyword"
    },
    "repositoryName": {
      "type": "text"
    },
    "projectId": {
      "type": "keyword"
    },
    "projectName": {
      "type": "text"
    },
    "namespace": {
      "type": "text"
    },
    "typeName": {
      "type": "text"
    },
    "symbolKind": {
      "type": "keyword"
    },
    "symbolName": {
      "type": "text"
    },
    "filePath": {
      "type": "text"
    },
    "chunkType": {
      "type": "keyword"
    },
    "contentHash": {
      "type": "keyword"
    },
    "tokenCount": {
      "type": "integer"
    },
    "lineStart": {
      "type": "integer"
    },
    "lineEnd": {
      "type": "integer"
    },
    "embeddingModel": {
      "type": "keyword"
    },
    "embeddingVersion": {
      "type": "integer"
    },
    "createdAt": {
      "type": "datetime"
    },
    "updatedAt": {
      "type": "datetime"
    }
  }
}
```

### 2.2 Payload Examples

```json
// Type Chunk
{
  "repositoryId": "abc123",
  "repositoryName": "NexusCode",
  "projectId": "def456",
  "projectName": "NexusCode.Domain",
  "namespace": "NexusCode.Domain.Models",
  "typeName": "PlayerController",
  "symbolKind": "class",
  "symbolName": "PlayerController",
  "filePath": "src/Player/PlayerController.cs",
  "chunkType": "type",
  "contentHash": "sha256hash",
  "tokenCount": 450,
  "lineStart": 10,
  "lineEnd": 250,
  "embeddingModel": "nomic-embed-text",
  "embeddingVersion": 1,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}

// Method Chunk
{
  "repositoryId": "abc123",
  "repositoryName": "NexusCode",
  "projectId": "def456",
  "projectName": "NexusCode.Domain",
  "namespace": "NexusCode.Domain.Models",
  "typeName": "PlayerController",
  "symbolKind": "method",
  "symbolName": "PlayerController.Attack()",
  "filePath": "src/Player/PlayerController.cs",
  "chunkType": "method",
  "contentHash": "sha256hash",
  "tokenCount": 120,
  "lineStart": 45,
  "lineEnd": 80,
  "embeddingModel": "nomic-embed-text",
  "embeddingVersion": 1,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}

// Graph Context Chunk
{
  "repositoryId": "abc123",
  "repositoryName": "NexusCode",
  "projectId": "def456",
  "projectName": "NexusCode.Domain",
  "namespace": "NexusCode.Domain.Models",
  "typeName": "PlayerController",
  "symbolKind": "method",
  "symbolName": "PlayerController.Attack()",
  "filePath": "src/Player/PlayerController.cs",
  "chunkType": "graphContext",
  "contentHash": "sha256hash",
  "tokenCount": 180,
  "lineStart": 45,
  "lineEnd": 80,
  "embeddingModel": "nomic-embed-text",
  "embeddingVersion": 1,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

### 2.3 Vector Store Interface

```csharp
interface IVectorStore
{
    // Collection management
    Task CreateCollection(string name, int dimension, string distance);
    Task DeleteCollection(string name);
    Task<List<string>> ListCollections();
    
    // Vector operations
    Task Upsert(string collection, VectorPoint point);
    Task UpsertBatch(string collection, List<VectorPoint> points);
    Task Delete(string collection, string id);
    Task DeleteBatch(string collection, List<string> ids);
    
    // Search
    Task<List<SearchResult>> Search(
        string collection,
        float[] queryVector,
        Filter? filter = null,
        int limit = 10,
        float? scoreThreshold = null);
    
    // Scroll (paginated retrieval)
    Task<List<VectorPoint>> Scroll(
        string collection,
        Filter? filter = null,
        int limit = 100,
        string? offset = null);
}

class VectorPoint
{
    string Id;
    float[] Vector;
    Dictionary<string, object> Payload;
}

class Filter
{
    Dictionary<string, FieldFilter> Must;
    Dictionary<string, FieldFilter> Should;
    Dictionary<string, FieldFilter> MustNot;
}

class FieldFilter
{
    string Key;
    FieldCondition Condition;
}

class FieldCondition
{
    // Match exact value
    object? Match;
    
    // Match text
    string? MatchText;
    
    // Range
    Range? Range;
    
    // List
    List<object>? In;
}

class SearchResult
{
    string Id;
    float Score;
    Dictionary<string, object>? Payload;
}
```

---

## 3. Graph RAG

### 3.1 Pipeline

```
Question
    ↓
┌─────────────────┐
│ Symbol Search    │  Find relevant symbols
└────────┬────────┘
         ↓
┌─────────────────┐
│ Vector Search    │  Find similar code
└────────┬────────┘
         ↓
┌─────────────────┐
│ Graph Expansion  │  Traverse relationships
└────────┬────────┘
         ↓
┌─────────────────┐
│ Evidence         │  Collect evidence paths
│ Collection       │
└────────┬────────┘
         ↓
┌─────────────────┐
│ Context          │  Aggregate and deduplicate
│ Aggregation      │
└────────┬────────┘
         ↓
┌─────────────────┐
│ Context          │  Fit within token limits
│ Compression      │
└────────┬────────┘
         ↓
┌─────────────────┐
│ Prompt Builder   │  Generate LLM prompt
└─────────────────┘
         ↓
LLM Response
```

### 3.2 Graph RAG Implementation

```
class GraphRAGEngine
{
    symbolSearch: SymbolSearchEngine
    vectorStore: IVectorStore
    knowledgeGraph: KnowledgeGraph
    contextBuilder: ContextBuilder
    
    async Answer(question: string, options: GraphRAGOptions):
        // Phase 1: Symbol Search
        symbols = await SearchSymbols(question, options)
        
        // Phase 2: Vector Search
        vectorResults = await SearchSimilar(question, options)
        
        // Phase 3: Graph Expansion
        graphExpansion = await ExpandGraph(symbols, options)
        
        // Phase 4: Evidence Collection
        evidence = CollectEvidence(symbols, vectorResults, graphExpansion)
        
        // Phase 5: Context Aggregation
        context = AggregateContext(evidence, options)
        
        // Phase 6: Context Compression
        compressed = CompressContext(context, options.MaxTokens)
        
        // Phase 7: Prompt Building
        prompt = BuildPrompt(question, compressed, options)
        
        return new GraphRAGResult {
            Prompt = prompt,
            Context = compressed,
            Evidence = evidence,
            TokenCount = compressed.TokenCount
        }
    
    async SearchSymbols(question: string, options: GraphRAGOptions):
        // Extract key terms from question
        terms = ExtractTerms(question)
        
        symbols = new List<SymbolContext>()
        
        foreach term in terms:
            matches = symbolSearch.FindSymbol(term, new SearchOptions {
                MaxResults = 5
            })
            symbols.AddRange(matches.Select(m => new SymbolContext {
                Symbol = m.Symbol,
                RelevanceScore = m.Score
            }))
        
        return symbols.DistinctBy(s => s.Symbol.Id)
            .OrderByDescending(s => s.RelevanceScore)
            .Take(options.MaxSymbols)
            .ToList()
    
    async SearchSimilar(question: string, options: GraphRAGOptions):
        // Generate embedding for question
        questionEmbedding = await GenerateEmbedding(question)
        
        // Search vector store
        results = await vectorStore.Search(
            collection: "code_chunks",
            queryVector: questionEmbedding,
            limit: options.MaxVectorResults,
            filter: new Filter {
                Must = new Dictionary<string, FieldFilter> {
                    ["repositoryId"] = new FieldFilter {
                        Key = "repositoryId",
                        Condition = new FieldCondition {
                            Match = options.RepositoryId
                        }
                    }
                }
            }
        )
        
        return results.Select(r => new VectorContext {
            ChunkId = r.Id,
            Score = r.Score,
            Payload = r.Payload
        }).ToList()
    
    async ExpandGraph(symbols: List<SymbolContext>, options: GraphRAGOptions):
        expansion = new GraphExpansion()
        
        foreach symbol in symbols:
            nodeId = GraphNodeId.FromFullName(symbol.Symbol.FullName)
            
            // BFS to expand graph
            traversal = knowledgeGraph.BFS(nodeId, options.GraphDepth, null)
            
            foreach item in traversal:
                node = knowledgeGraph.GetNode(item.NodeId)
                if node != null:
                    expansion.AddNode(new ExpandedNode {
                        Node = node,
                        Depth = item.Depth,
                        RelevanceScore = CalculateRelevance(item, symbol)
                    })
        
        return expansion
    
    CollectEvidence(
        symbols: List<SymbolContext>,
        vectorResults: List<VectorContext>,
        graphExpansion: GraphExpansion):
        
        evidence = new EvidenceCollection()
        
        // Add symbol evidence
        foreach symbol in symbols:
            evidence.Add(new Evidence {
                Type = EvidenceType.Symbol,
                Symbol = symbol.Symbol,
                Score = symbol.RelevanceScore,
                Source = "symbol_search"
            })
        
        // Add vector evidence
        foreach vector in vectorResults:
            evidence.Add(new Evidence {
                Type = EvidenceType.Code,
                ChunkId = vector.ChunkId,
                Score = vector.Score,
                Source = "vector_search"
            })
        
        // Add graph evidence
        foreach node in graphExpansion.Nodes:
            evidence.Add(new Evidence {
                Type = EvidenceType.Relationship,
                Node = node.Node,
                Score = node.RelevanceScore,
                Source = "graph_expansion"
            })
        
        // Deduplicate
        evidence.Deduplicate()
        
        // Rank by score
        evidence.RankByScore()
        
        return evidence
    
    AggregateContext(evidence: EvidenceCollection, options: GraphRAGOptions):
        context = new AggregatedContext()
        
        // Group evidence by type
        symbolEvidence = evidence.Where(e => e.Type == EvidenceType.Symbol)
        codeEvidence = evidence.Where(e => e.Type == EvidenceType.Code)
        relationshipEvidence = evidence.Where(e => e.Type == EvidenceType.Relationship)
        
        // Add symbols with their code
        foreach item in symbolEvidence:
            code = GetSymbolCode(item.Symbol)
            context.AddSymbol(new ContextSymbol {
                Symbol = item.Symbol,
                Code = code,
                RelevanceScore = item.Score
            })
        
        // Add code snippets
        foreach item in codeEvidence:
            chunk = await GetChunk(item.ChunkId)
            context.AddCodeSnippet(new ContextCodeSnippet {
                Content = chunk.Content,
                FilePath = chunk.FilePath,
                RelevanceScore = item.Score
            })
        
        // Add relationships
        foreach item in relationshipEvidence:
            context.AddRelationship(new ContextRelationship {
                From = item.Node,
                To = GetConnectedNodes(item.Node),
                Kind = item.EdgeKind
            })
        
        return context
    
    CompressContext(context: AggregatedContext, maxTokens: int):
        compressor = new ContextCompressor()
        return compressor.Compress(context, maxTokens)
    
    BuildPrompt(question: string, context: AggregatedContext, options: GraphRAGOptions):
        builder = new PromptBuilder()
        
        prompt = new StringBuilder()
        
        // System prompt
        prompt.AppendLine("You are a C# code expert. Answer the question based on the provided code context.")
        prompt.AppendLine()
        
        // Question
        prompt.AppendLine($"Question: {question}")
        prompt.AppendLine()
        
        // Relevant Symbols
        if context.Symbols.Count > 0:
            prompt.AppendLine("## Relevant Code Symbols")
            prompt.AppendLine()
            
            foreach symbol in context.Symbols:
                prompt.AppendLine($"### {symbol.Symbol.FullName}")
                prompt.AppendLine($"- File: {symbol.Symbol.FilePath}:{symbol.Symbol.StartLine}")
                
                if symbol.Code != null:
                    prompt.AppendLine("```csharp")
                    prompt.AppendLine(symbol.Code)
                    prompt.AppendLine("```")
                prompt.AppendLine()
        
        // Relationships
        if context.Relationships.Count > 0:
            prompt.AppendLine("## Code Relationships")
            prompt.AppendLine()
            
            foreach rel in context.Relationships:
                prompt.AppendLine($"- {rel.From.Label} → {rel.To.Label}: {rel.Kind}")
            prompt.AppendLine()
        
        // Code Snippets
        if context.CodeSnippets.Count > 0:
            prompt.AppendLine("## Related Code")
            prompt.AppendLine()
            
            foreach snippet in context.CodeSnippets:
                prompt.AppendLine($"### {snippet.FilePath}:{snippet.StartLine}")
                prompt.AppendLine("```csharp")
                prompt.AppendLine(snippet.Content)
                prompt.AppendLine("```")
                prompt.AppendLine()
        
        // Instructions
        prompt.AppendLine("Based on the above context, provide a comprehensive answer.")
        prompt.AppendLine("If the context doesn't contain enough information, state what's missing.")
        
        return prompt.ToString()
    }
}
```

### 3.3 Evidence Collection

```csharp
enum EvidenceType
{
    Symbol,
    Code,
    Relationship,
    Dependency,
    Reference
}

class Evidence
{
    EvidenceType Type;
    SymbolInfo? Symbol;
    GraphNode? Node;
    string? ChunkId;
    double Score;
    string Source;
    EdgeKind? EdgeKind;
}

class EvidenceCollection
{
    List<Evidence> _evidence;
    
    Add(Evidence evidence)
    {
        // Check for duplicates
        if (!IsDuplicate(evidence))
        {
            _evidence.Add(evidence);
        }
    }
    
    IsDuplicate(Evidence newEvidence)
    {
        return _evidence.Any(e =>
            e.Type == newEvidence.Type &&
            e.Symbol?.Id == newEvidence.Symbol?.Id &&
            e.Node?.Id == newEvidence.Node?.Id &&
            e.ChunkId == newEvidence.ChunkId
        );
    }
    
    Deduplicate()
    {
        _evidence = _evidence
            .GroupBy(e => new { e.Type, SymbolId = e.Symbol?.Id, NodeId = e.Node?.Id, e.ChunkId })
            .Select(g => g.OrderByDescending(e => e.Score).First())
            .ToList();
    }
    
    RankByScore()
    {
        _evidence = _evidence
            .OrderByDescending(e => e.Score)
            .ToList();
    }
}
```

### 3.4 Complexity Analysis

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|-----------------|
| Symbol Search | O(S) where S = symbol count | O(R) where R = results |
| Vector Search | O(V) where V = vector count | O(R) |
| Graph Expansion | O(N * D) where N = nodes, D = depth | O(N * D) |
| Evidence Collection | O(E) where E = evidence count | O(E) |
| Context Aggregation | O(E) | O(C) where C = context size |
| Context Compression | O(C) | O(C) |
| Prompt Building | O(C) | O(P) where P = prompt size |
