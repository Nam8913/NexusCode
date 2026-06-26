using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class HybridSearchEngine
{
    private readonly SymbolSearchEngine _symbolSearch;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;

    public HybridSearchEngine(SymbolSearchEngine symbolSearch, SymbolTable symbolTable, KnowledgeGraph graph)
    {
        _symbolSearch = symbolSearch;
        _symbolTable = symbolTable;
        _graph = graph;
    }

    public List<HybridSearchResult> Search(string query, int topK = 10)
    {
        int k = 60;
        int candidateK = topK * 3;

        var fuzzyResults = _symbolSearch.FindSymbol(query, new SearchOptions { MaxResults = candidateK });

        var scores = new Dictionary<Guid, double>();
        var symbolMap = new Dictionary<Guid, SymbolEntity>();

        for (int i = 0; i < fuzzyResults.Count; i++)
        {
            var result = fuzzyResults[i];
            var rrfScore = 1.0 / (k + i + 1);

            scores.TryGetValue(result.Symbol.Id, out var existing);
            scores[result.Symbol.Id] = existing + rrfScore;
            symbolMap[result.Symbol.Id] = result.Symbol;
        }

        var graphExpanded = ExpandFromQuery(query, candidateK);
        foreach (var (symbolId, graphScore) in graphExpanded)
        {
            scores.TryGetValue(symbolId, out var existing);
            scores[symbolId] = existing + graphScore * 0.5;
            if (!symbolMap.ContainsKey(symbolId))
            {
                var sym = _symbolTable.GetById(symbolId);
                if (sym != null) symbolMap[symbolId] = sym;
            }
        }

        return scores
            .OrderByDescending(kv => kv.Value)
            .Take(topK)
            .Where(kv => symbolMap.ContainsKey(kv.Key))
            .Select(kv => new HybridSearchResult
            {
                Symbol = symbolMap[kv.Key],
                Score = kv.Value,
                FuzzyScore = fuzzyResults.FirstOrDefault(f => f.Symbol.Id == kv.Key)?.Score ?? 0,
                GraphScore = graphExpanded.ContainsKey(kv.Key) ? graphExpanded[kv.Key] : 0
            })
            .ToList();
    }

    private Dictionary<Guid, double> ExpandFromQuery(string query, int maxResults)
    {
        var results = new Dictionary<Guid, double>();
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var term in terms)
        {
            var matches = _symbolSearch.FindSymbol(term, new SearchOptions { MaxResults = 5 });
            foreach (var match in matches)
            {
                var neighbors = GetGraphNeighbors(match.Symbol.Id, 2);
                foreach (var (neighborId, depth) in neighbors)
                {
                    var score = 1.0 / (1.0 + depth);
                    results.TryGetValue(neighborId, out var existing);
                    results[neighborId] = Math.Max(existing, score);
                }
            }
        }

        return results;
    }

    private List<(Guid Id, int Depth)> GetGraphNeighbors(Guid nodeId, int maxDepth)
    {
        var neighbors = new List<(Guid, int)>();
        var visited = new HashSet<Guid>();
        var queue = new Queue<(Guid, int)>();

        queue.Enqueue((nodeId, 0));
        visited.Add(nodeId);

        while (queue.Count > 0 && neighbors.Count < 50)
        {
            var (currentId, depth) = queue.Dequeue();
            if (depth >= maxDepth) continue;

            var outgoing = _graph.GetOutgoingEdges(currentId.ToByteArray());
            foreach (var edge in outgoing)
            {
                var targetId = new Guid(edge.TargetId);
                if (!visited.Contains(targetId))
                {
                    visited.Add(targetId);
                    neighbors.Add((targetId, depth + 1));
                    queue.Enqueue((targetId, depth + 1));
                }
            }

            var incoming = _graph.GetIncomingEdges(currentId.ToByteArray());
            foreach (var edge in incoming)
            {
                var sourceId = new Guid(edge.SourceId);
                if (!visited.Contains(sourceId))
                {
                    visited.Add(sourceId);
                    neighbors.Add((sourceId, depth + 1));
                    queue.Enqueue((sourceId, depth + 1));
                }
            }
        }

        return neighbors;
    }
}

public class HybridSearchResult
{
    public SymbolEntity Symbol { get; set; } = new();
    public double Score { get; set; }
    public double FuzzyScore { get; set; }
    public double GraphScore { get; set; }
}
