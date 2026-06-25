# Nexus Code Intelligence Platform - Context Builder

## Overview

The Context Builder transforms natural language questions into structured context for LLM consumption. It combines Symbol Search, Graph Expansion, and Vector Search to gather relevant code information.

---

## 1. Pipeline

```
Question
    ↓
┌─────────────────┐
│ Question Parser  │  Extract intent, entities, keywords
└────────┬────────┘
         ↓
┌─────────────────┐
│ Symbol Search    │  Find relevant symbols
└────────┬────────┘
         ↓
┌─────────────────┐
│ Graph Expansion  │  Traverse relationships
└────────┬────────┘
         ↓
┌─────────────────┐
│ Vector Search    │  Find similar code (optional)
└────────┬────────┘
         ↓
┌─────────────────┐
│ Context Builder  │  Assemble context pieces
└────────┬────────┘
         ↓
┌─────────────────┐
│ Token Counter   │  Estimate token usage
└────────┬────────┘
         ↓
┌─────────────────┐
│ Context Compressor│  Fit within token limits
└────────┬────────┘
         ↓
┌─────────────────┐
│ Prompt Builder   │  Generate LLM prompt
└─────────────────┘
         ↓
Output (Context Window)
```

---

## 2. Question Parser

```
class QuestionParser
{
    Parse(question: string):
        result = new ParsedQuestion()
        
        // Extract intent
        result.Intent = ExtractIntent(question)
        
        // Extract entities (type names, method names, etc.)
        result.Entities = ExtractEntities(question)
        
        // Extract keywords
        result.Keywords = ExtractKeywords(question)
        
        // Determine search strategy
        result.Strategy = DetermineStrategy(result)
        
        return result
    
    ExtractIntent(question: string):
        // Classify question intent
        lowerQuestion = question.ToLower()
        
        if lowerQuestion.Contains("how") || lowerQuestion.Contains("explain"):
            return Intent.Explanation
        elif lowerQuestion.Contains("where") || lowerQuestion.Contains("find"):
            return Intent.Location
        elif lowerQuestion.Contains("what") && lowerQuestion.Contains("call"):
            return Intent.CallGraph
        elif lowerQuestion.Contains("depend") || lowerQuestion.Contains("use"):
            return Intent.Dependencies
        elif lowerQuestion.Contains("inherit") || lowerQuestion.Contains("implement"):
            return Intent.Hierarchy
        elif lowerQuestion.Contains("debug") || lowerQuestion.Contains("error"):
            return Intent.Debugging
        else:
            return Intent.General
    
    ExtractEntities(question: string):
        entities = new List<Entity>()
        
        // Look for capitalized words (likely type names)
        words = question.Split(' ')
        foreach word in words:
            if IsPascalCase(word):
                entities.Add(new Entity {
                    Name = word,
                    Type = EntityType.Type
                })
        
        // Look for method-like patterns (word followed by parentheses)
        methodPattern = new Regex(@"(\w+)\s*\(")
        matches = methodPattern.Matches(question)
        foreach match in matches:
            entities.Add(new Entity {
                Name = match.Groups[1].Value,
                Type = EntityType.Method
            })
        
        return entities
    
    ExtractKeywords(question: string):
        // Remove common stop words
        stopWords = ["the", "a", "an", "is", "are", "was", "were", "be", "been",
                     "being", "have", "has", "had", "do", "does", "did", "will",
                     "would", "could", "should", "may", "might", "shall", "can",
                     "this", "that", "these", "those", "i", "you", "he", "she",
                     "it", "we", "they", "me", "him", "her", "us", "them"]
        
        words = question.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !stopWords.Contains(w.ToLower()))
            .Select(w => w.ToLower())
            .Distinct()
        
        return words.ToList()
    
    DetermineStrategy(parsed: ParsedQuestion):
        if parsed.Entities.Count > 0:
            return SearchStrategy.SymbolFirst
        elif parsed.Intent == Intent.CallGraph:
            return SearchStrategy.GraphFirst
        elif parsed.Intent == Intent.Dependencies:
            return SearchStrategy.GraphFirst
        else:
            return SearchStrategy.Hybrid
    }
```

---

## 3. Symbol Search Phase

```
class ContextSymbolSearch
{
    symbolSearch: SymbolSearchEngine
    
    SearchSymbols(parsed: ParsedQuestion, maxSymbols: int):
        results = new List<SymbolContext>()
        
        // Search by entities
        foreach entity in parsed.Entities:
            matches = symbolSearch.FindSymbol(entity.Name, new SearchOptions {
                MaxResults = 5,
                KindFilter = MapEntityType(entity.Type)
            })
            
            foreach match in matches:
                results.Add(new SymbolContext {
                    Symbol = match.Symbol,
                    RelevanceScore = match.Score,
                    Source = "entity_search"
                })
        
        // Search by keywords
        foreach keyword in parsed.Keywords:
            matches = symbolSearch.FindSymbol(keyword, new SearchOptions {
                MaxResults = 3
            })
            
            foreach match in matches:
                if !results.Any(r => r.Symbol.Id == match.Symbol.Id):
                    results.Add(new SymbolContext {
                        Symbol = match.Symbol,
                        RelevanceScore = match.Score * 0.7, // lower weight for keyword matches
                        Source = "keyword_search"
                    })
        
        // Sort by relevance and take top results
        return results
            .OrderByDescending(r => r.RelevanceScore)
            .Take(maxSymbols)
            .ToList()
    
    MapEntityType(entityType: EntityType):
        return entityType switch {
            EntityType.Type => SymbolKind.Type,
            EntityType.Method => SymbolKind.Method,
            EntityType.Property => SymbolKind.Property,
            EntityType.Field => SymbolKind.Field,
            _ => null
        }
}
```

---

## 4. Graph Expansion Phase

```
class ContextGraphExpansion
{
    knowledgeGraph: KnowledgeGraph
    
    ExpandFromSymbols(symbols: List<SymbolContext>, maxDepth: int, maxNodes: int):
        expandedNodes = new List<GraphNodeContext>()
        visitedNodes = new HashSet<GraphNodeId>()
        
        foreach symbolContext in symbols:
            nodeId = GraphNodeId.FromFullName(symbolContext.Symbol.FullName)
            
            // BFS from this node
            expansion = ExpandNode(nodeId, maxDepth, visitedNodes)
            expandedNodes.AddRange(expansion)
        
        // Deduplicate and rank
        return expandedNodes
            .GroupBy(n => n.Node.Id)
            .Select(g => g.First())
            .OrderByDescending(n => n.RelevanceScore)
            .Take(maxNodes)
            .ToList()
    
    ExpandNode(nodeId: GraphNodeId, maxDepth: int, visited: HashSet):
        results = new List<GraphNodeContext>()
        
        if maxDepth <= 0 || visited.Contains(nodeId):
            return results
        
        visited.Add(nodeId)
        node = knowledgeGraph.GetNode(nodeId)
        
        if node == null:
            return results
        
        // Get all edges
        outgoingEdges = knowledgeGraph.GetOutgoingEdges(nodeId)
        incomingEdges = knowledgeGraph.GetIncomingEdges(nodeId)
        
        // Add current node
        results.Add(new GraphNodeContext {
            Node = node,
            Depth = 0,
            RelevanceScore = 1.0
        })
        
        // Expand outgoing edges
        foreach edge in outgoingEdges:
            targetNode = knowledgeGraph.GetNode(edge.TargetId)
            if targetNode != null:
                results.Add(new GraphNodeContext {
                    Node = targetNode,
                    Depth = 1,
                    RelevanceScore = CalculateEdgeRelevance(edge),
                    EdgeFrom = nodeId,
                    EdgeKind = edge.Kind
                })
                
                // Recursively expand
                if maxDepth > 1:
                    subExpansion = ExpandNode(edge.TargetId, maxDepth - 1, visited)
                    foreach item in subExpansion:
                        item.Depth += 1
                        item.RelevanceScore *= 0.5 // discount for distance
                    results.AddRange(subExpansion)
        
        // Expand incoming edges (callers, etc.)
        foreach edge in incomingEdges:
            sourceNode = knowledgeGraph.GetNode(edge.SourceId)
            if sourceNode != null:
                results.Add(new GraphNodeContext {
                    Node = sourceNode,
                    Depth = 1,
                    RelevanceScore = CalculateEdgeRelevance(edge) * 0.8,
                    EdgeTo = nodeId,
                    EdgeKind = edge.Kind
                })
        
        return results
    
    CalculateEdgeRelevance(edge: GraphEdge):
        // Higher relevance for certain edge types
        return edge.Kind switch {
            EdgeKind.CALLS => 0.9,
            EdgeKind.INHERITS => 0.8,
            EdgeKind.IMPLEMENTS => 0.8,
            EdgeKind.CONTAINS => 0.7,
            EdgeKind.DECLARES => 0.7,
            EdgeKind.USES => 0.6,
            EdgeKind.REFERENCES => 0.5,
            _ => 0.5
        }
}
```

---

## 5. Vector Search Phase (Optional)

```
class ContextVectorSearch
{
    vectorStore: IVectorStore
    
    SearchSimilar(symbols: List<SymbolContext>, maxResults: int):
        results = new List<VectorContext>()
        
        foreach symbolContext in symbols:
            // Find similar code using vector similarity
            similarChunks = vectorStore.Search(
                collection: "code_chunks",
                queryVector: GetEmbeddingForSymbol(symbolContext.Symbol),
                filter: new Filter {
                    RepositoryId = symbolContext.Symbol.RepositoryId
                },
                limit: 5
            )
            
            foreach chunk in similarChunks:
                results.Add(new VectorContext {
                    Chunk = chunk,
                    RelevanceScore = chunk.Score,
                    SourceSymbol = symbolContext.Symbol
                })
        
        return results
            .OrderByDescending(r => r.RelevanceScore)
            .Take(maxResults)
            .ToList()
    
    GetEmbeddingForSymbol(symbol: SymbolInfo):
        // Get or generate embedding for this symbol
        // Use the symbol's code content
        return vectorStore.GetEmbedding(symbol.FullName)
    }
```

---

## 6. Context Aggregation

```
class ContextAggregator
{
    Aggregate(
        symbols: List<SymbolContext>,
        graphNodes: List<GraphNodeContext>,
        vectorResults: List<VectorContext>,
        options: ContextOptions):
        
        context = new AggregatedContext()
        
        // Add symbols
        foreach symbol in symbols:
            context.AddSymbol(new ContextSymbol {
                Symbol = symbol.Symbol,
                RelevanceScore = symbol.RelevanceScore,
                IncludeCode = options.IncludeCode,
                IncludeReferences = options.IncludeReferences
            })
        
        // Add graph nodes
        foreach node in graphNodes:
            if !context.HasNode(node.Node.Id):
                context.AddNode(new ContextNode {
                    Node = node.Node,
                    RelevanceScore = node.RelevanceScore,
                    Relationships = GetRelationships(node, graphNodes)
                })
        
        // Add vector results
        foreach vector in vectorResults:
            context.AddCodeSnippet(new ContextCodeSnippet {
                Chunk = vector.Chunk,
                RelevanceScore = vector.RelevanceScore,
                SourceSymbol = vector.SourceSymbol
            })
        
        // Deduplicate
        context.Deduplicate()
        
        // Rank by relevance
        context.RankByRelevance()
        
        return context
    
    GetRelationships(node: GraphNodeContext, allNodes: List<GraphNodeContext>):
        relationships = new List<ContextRelationship>()
        
        // Find edges to other nodes in context
        outgoingEdges = knowledgeGraph.GetOutgoingEdges(node.Node.Id)
        foreach edge in outgoingEdges:
            if allNodes.Any(n => n.Node.Id == edge.TargetId):
                relationships.Add(new ContextRelationship {
                    From = node.Node.Id,
                    To = edge.TargetId,
                    Kind = edge.Kind
                })
        
        return relationships
    }
```

---

## 7. Context Compression

```
class ContextCompressor
{
    tokenCounter: TokenCounter
    
    Compress(context: AggregatedContext, maxTokens: int):
        currentTokens = tokenCounter.Count(context)
        
        if currentTokens <= maxTokens:
            return context
        
        // Strategy 1: Remove low-relevance items
        compressed = RemoveLowRelevance(context, maxTokens)
        currentTokens = tokenCounter.Count(compressed)
        
        if currentTokens <= maxTokens:
            return compressed
        
        // Strategy 2: Truncate code snippets
        compressed = TruncateSnippets(compressed, maxTokens)
        currentTokens = tokenCounter.Count(compressed)
        
        if currentTokens <= maxTokens:
            return compressed
        
        // Strategy 3: Remove graph relationships
        compressed = SimplifyGraph(compressed, maxTokens)
        currentTokens = tokenCounter.Count(compressed)
        
        if currentTokens <= maxTokens:
            return compressed
        
        // Strategy 4: Keep only top-N symbols
        compressed = KeepTopSymbols(compressed, maxTokens)
        
        return compressed
    
    RemoveLowRelevance(context: AggregatedContext, maxTokens: int):
        // Remove items below relevance threshold
        threshold = CalculateThreshold(context, maxTokens)
        
        context.Symbols.RemoveAll(s => s.RelevanceScore < threshold)
        context.Nodes.RemoveAll(n => n.RelevanceScore < threshold)
        context.CodeSnippets.RemoveAll(c => c.RelevanceScore < threshold)
        
        return context
    
    TruncateSnippets(context: AggregatedContext, maxTokens: int):
        foreach snippet in context.CodeSnippets:
            snippet.Content = TruncateToTokenLimit(snippet.Content, maxTokens / context.CodeSnippets.Count)
        
        return context
    
    SimplifyGraph(context: AggregatedContext, maxTokens: int):
        // Keep only direct relationships
        foreach node in context.Nodes:
            node.Relationships = node.Relationships
                .Where(r => r.Distance <= 1)
                .ToList()
        
        return context
    
    KeepTopSymbols(context: AggregatedContext, maxTokens: int):
        // Calculate how many symbols we can keep
        avgTokensPerSymbol = EstimateTokensPerSymbol()
        maxSymbols = maxTokens / avgTokensPerSymbol
        
        context.Symbols = context.Symbols
            .OrderByDescending(s => s.RelevanceScore)
            .Take((int)maxSymbols)
            .ToList()
        
        return context
    }
```

---

## 8. Prompt Builder

```
class PromptBuilder
{
    BuildPrompt(context: AggregatedContext, question: string, options: PromptOptions):
        prompt = new StringBuilder()
        
        // System context
        prompt.AppendLine("You are a C# code expert helping analyze a codebase.")
        prompt.AppendLine()
        
        // Question
        prompt.AppendLine($"Question: {question}")
        prompt.AppendLine()
        
        // Relevant Symbols
        if context.Symbols.Count > 0:
            prompt.AppendLine("## Relevant Symbols")
            prompt.AppendLine()
            
            foreach symbol in context.Symbols:
                prompt.AppendLine($"### {symbol.Symbol.FullName}")
                prompt.AppendLine($"- Kind: {symbol.Symbol.Kind}")
                prompt.AppendLine($"- File: {symbol.Symbol.FilePath}:{symbol.Symbol.StartLine}")
                
                if options.IncludeCode && symbol.Code != null:
                    prompt.AppendLine($"```csharp")
                    prompt.AppendLine(symbol.Code)
                    prompt.AppendLine($"```")
                
                prompt.AppendLine()
        
        // Graph Relationships
        if options.IncludeGraph && context.Nodes.Count > 0:
            prompt.AppendLine("## Code Relationships")
            prompt.AppendLine()
            
            // Build Mermaid diagram
            prompt.AppendLine("```mermaid")
            prompt.AppendLine("graph TD")
            
            foreach node in context.Nodes:
                prompt.AppendLine($"    {node.Node.Id}["{node.Node.Label}"]")
            
            foreach node in context.Nodes:
                foreach relationship in node.Relationships:
                    prompt.AppendLine($"    {relationship.From} --> {relationship.To}")
            
            prompt.AppendLine("```")
            prompt.AppendLine()
        
        // Code Snippets
        if options.IncludeCode && context.CodeSnippets.Count > 0:
            prompt.AppendLine("## Related Code")
            prompt.AppendLine()
            
            foreach snippet in context.CodeSnippets:
                prompt.AppendLine($"### {snippet.FilePath}:{snippet.StartLine}")
                prompt.AppendLine($"```csharp")
                prompt.AppendLine(snippet.Content)
                prompt.AppendLine($"```")
                prompt.AppendLine()
        
        // Instructions
        prompt.AppendLine("Based on the above code context, please answer the question.")
        prompt.AppendLine("If you need more information, specify what additional context would be helpful.")
        
        return prompt.ToString()
    }
```

---

## 9. Token Counter

```
class TokenCounter
{
    // Approximate token counting (1 token ≈ 4 characters for English, ≈ 2 for C#)
    Count(text: string):
        if string.IsNullOrEmpty(text):
            return 0
        
        // Simple heuristic
        return (int)Math.Ceiling(text.Length / 3.0)
    
    CountContext(context: AggregatedContext):
        total = 0
        
        // Count symbols
        foreach symbol in context.Symbols:
            total += Count(symbol.Symbol.FullName)
            if symbol.Code != null:
                total += Count(symbol.Code)
        
        // Count graph nodes
        foreach node in context.Nodes:
            total += Count(node.Node.Label)
            total += Count(node.Node.FullName)
        
        // Count code snippets
        foreach snippet in context.CodeSnippets:
            total += Count(snippet.Content)
        
        return total
    
    EstimateTokensPerSymbol():
        // Average tokens per symbol with code
        return 200 // rough estimate
    }
```

---

## 10. Complexity Analysis

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|-----------------|
| Question Parsing | O(W) where W = words | O(W) |
| Symbol Search | O(S) where S = symbol count | O(R) where R = results |
| Graph Expansion | O(N * D) where N = nodes, D = depth | O(N * D) |
| Vector Search | O(V) where V = vector count | O(V) |
| Context Aggregation | O(S + N + V) | O(S + N + V) |
| Context Compression | O(C) where C = context size | O(C) |
| Prompt Building | O(C) | O(P) where P = prompt size |
