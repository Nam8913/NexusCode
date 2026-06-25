using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Context;

public sealed class ContextBuilder
{
    private readonly SymbolSearchEngine _searchEngine;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;

    public ContextBuilder(SymbolSearchEngine searchEngine, SymbolTable symbolTable, KnowledgeGraph graph)
    {
        _searchEngine = searchEngine;
        _symbolTable = symbolTable;
        _graph = graph;
    }

    public ContextResult BuildContext(string question, int maxTokens = 4000)
    {
        var result = new ContextResult { Question = question };

        var terms = ExtractTerms(question);
        var symbols = new List<SymbolEntity>();

        foreach (var term in terms)
        {
            var matches = _searchEngine.FindSymbol(term, new SearchOptions { MaxResults = 5 });
            symbols.AddRange(matches.Select(m => m.Symbol));
        }

        symbols = symbols.DistinctBy(s => s.Id).Take(20).ToList();

        foreach (var symbol in symbols)
        {
            result.Symbols.Add(new ContextSymbol
            {
                Symbol = symbol,
                RelevanceScore = 1.0
            });

            var callers = _searchEngine.FindCallers(symbol.Id, 1);
            foreach (var caller in callers.Take(5))
            {
                result.Relationships.Add(new ContextRelationship
                {
                    From = caller.Symbol.FullName,
                    To = symbol.FullName,
                    Kind = "calls"
                });
            }

            var callees = _searchEngine.FindCallees(symbol.Id, 1);
            foreach (var callee in callees.Take(5))
            {
                result.Relationships.Add(new ContextRelationship
                {
                    From = symbol.FullName,
                    To = callee.Symbol.FullName,
                    Kind = "calls"
                });
            }
        }

        result.TokenCount = EstimateTokens(result);
        return result;
    }

    public string BuildPrompt(ContextResult context)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You are a C# code expert. Answer based on the provided context.");
        sb.AppendLine();
        sb.AppendLine($"Question: {context.Question}");
        sb.AppendLine();

        if (context.Symbols.Count > 0)
        {
            sb.AppendLine("## Relevant Symbols");
            foreach (var s in context.Symbols)
            {
                sb.AppendLine($"- **{s.Symbol.FullName}** ({s.Symbol.Kind})");
                sb.AppendLine($"  File: {s.Symbol.FilePath}:{s.Symbol.StartLine}");
                if (s.Symbol.ReturnType != null)
                    sb.AppendLine($"  Returns: {s.Symbol.ReturnType}");
                sb.AppendLine();
            }
        }

        if (context.Relationships.Count > 0)
        {
            sb.AppendLine("## Relationships");
            foreach (var r in context.Relationships.Take(20))
            {
                sb.AppendLine($"- {r.From} → {r.To}: {r.Kind}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Provide a comprehensive answer based on the above context.");
        return sb.ToString();
    }

    private static List<string> ExtractTerms(string question)
    {
        var stopWords = new HashSet<string> { "the", "a", "an", "is", "are", "was", "how", "what", "does", "do", "in", "to", "for", "of", "and", "or", "not", "this", "that" };

        return question.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stopWords.Contains(w.ToLower()))
            .Select(w => w.Trim('.', ',', '?', '!'))
            .Where(w => w.Length > 0)
            .ToList();
    }

    private static int EstimateTokens(ContextResult context)
    {
        int tokens = 0;
        tokens += context.Question.Length / 4;
        tokens += context.Symbols.Count * 50;
        tokens += context.Relationships.Count * 20;
        return tokens;
    }
}

public class ContextResult
{
    public string Question { get; set; } = string.Empty;
    public List<ContextSymbol> Symbols { get; set; } = [];
    public List<ContextRelationship> Relationships { get; set; } = [];
    public int TokenCount { get; set; }
}

public class ContextSymbol
{
    public SymbolEntity Symbol { get; set; } = new();
    public double RelevanceScore { get; set; }
}

public class ContextRelationship
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}
