using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class GraphRAGEngine
{
    private readonly SymbolSearchEngine _searchEngine;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;

    public GraphRAGEngine(SymbolSearchEngine searchEngine, SymbolTable symbolTable, KnowledgeGraph graph)
    {
        _searchEngine = searchEngine;
        _symbolTable = symbolTable;
        _graph = graph;
    }

    public GraphRAGResult Answer(string question, int maxContextTokens = 4000)
    {
        var result = new GraphRAGResult { Question = question };

        var terms = ExtractTerms(question);
        var symbolIds = new HashSet<Guid>();

        foreach (var term in terms)
        {
            var matches = _searchEngine.FindSymbol(term, new SearchOptions { MaxResults = 5 });
            foreach (var match in matches)
            {
                if (symbolIds.Add(match.Symbol.Id))
                {
                    result.Evidence.Add(new GraphRAGEvidence
                    {
                        Symbol = match.Symbol,
                        Score = match.Score,
                        Source = "symbol_search"
                    });
                }
            }
        }

        foreach (var evidence in result.Evidence.ToList())
        {
            var callers = _searchEngine.FindCallers(evidence.Symbol.Id, 2);
            foreach (var caller in callers)
            {
                if (symbolIds.Add(caller.Symbol.Id))
                {
                    result.Evidence.Add(new GraphRAGEvidence
                    {
                        Symbol = caller.Symbol,
                        Score = caller.Depth == 1 ? 0.8 : 0.5,
                        Source = "graph_caller"
                    });
                }
            }

            var callees = _searchEngine.FindCallees(evidence.Symbol.Id, 2);
            foreach (var callee in callees)
            {
                if (symbolIds.Add(callee.Symbol.Id))
                {
                    result.Evidence.Add(new GraphRAGEvidence
                    {
                        Symbol = callee.Symbol,
                        Score = callee.Depth == 1 ? 0.8 : 0.5,
                        Source = "graph_callee"
                    });
                }
            }
        }

        result.Evidence = result.Evidence
            .OrderByDescending(e => e.Score)
            .Take(30)
            .ToList();

        result.Prompt = BuildPrompt(result);
        result.TokenCount = EstimateTokens(result);

        return result;
    }

    private string BuildPrompt(GraphRAGResult result)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You are a C# code expert. Answer based on the following code context.");
        sb.AppendLine();
        sb.AppendLine($"Question: {result.Question}");
        sb.AppendLine();

        sb.AppendLine("## Relevant Code Symbols");
        foreach (var e in result.Evidence.Where(e => e.Source == "symbol_search").Take(10))
        {
            sb.AppendLine($"### {e.Symbol.FullName}");
            sb.AppendLine($"- Kind: {e.Symbol.Kind}");
            sb.AppendLine($"- File: {e.Symbol.FilePath}:{e.Symbol.StartLine}");
            if (e.Symbol.ReturnType != null)
                sb.AppendLine($"- Returns: {e.Symbol.ReturnType}");
            sb.AppendLine();
        }

        sb.AppendLine("## Code Relationships");
        var relationships = result.Evidence
            .SelectMany(e => GetRelationships(e.Symbol))
            .Distinct()
            .Take(20);

        foreach (var r in relationships)
        {
            sb.AppendLine($"- {r.from} → {r.to}: {r.kind}");
        }
        sb.AppendLine();

        sb.AppendLine("Provide a comprehensive answer explaining how the code works.");
        return sb.ToString();
    }

    private IEnumerable<(string from, string to, string kind)> GetRelationships(SymbolEntity symbol)
    {
        var edges = _graph.GetOutgoingEdges(symbol.Id.ToByteArray());
        foreach (var edge in edges)
        {
            var targetNode = _graph.GetNode(edge.TargetId);
            if (targetNode != null)
            {
                yield return (symbol.FullName, targetNode.FullName, edge.Kind.ToString());
            }
        }
    }

    private static List<string> ExtractTerms(string question)
    {
        var stopWords = new HashSet<string> { "the", "a", "an", "is", "are", "was", "how", "what", "does", "do", "in", "to", "for", "of", "and", "or", "not", "this", "that", "why", "when", "where", "which" };

        return question.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stopWords.Contains(w.ToLower()))
            .Select(w => w.Trim('.', ',', '?', '!'))
            .Where(w => w.Length > 0)
            .ToList();
    }

    private static int EstimateTokens(GraphRAGResult result)
    {
        return result.Prompt.Length / 4;
    }
}

public class GraphRAGResult
{
    public string Question { get; set; } = string.Empty;
    public List<GraphRAGEvidence> Evidence { get; set; } = [];
    public string Prompt { get; set; } = string.Empty;
    public int TokenCount { get; set; }
}

public class GraphRAGEvidence
{
    public SymbolEntity Symbol { get; set; } = new();
    public double Score { get; set; }
    public string Source { get; set; } = string.Empty;
}
