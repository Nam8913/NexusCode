using System.Collections.Concurrent;
using System.IO;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class SymbolSearchEngine
{
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;

    private readonly ConcurrentDictionary<string, List<Guid>> _trigramIndex = new();

    public SymbolSearchEngine(SymbolTable symbolTable, KnowledgeGraph graph)
    {
        _symbolTable = symbolTable;
        _graph = graph;
        BuildTrigramIndex();
    }

    public IReadOnlyList<SearchResult> FindSymbol(string query, SearchOptions? options = null)
    {
        options ??= new SearchOptions();
        var results = new List<SearchResult>();

        var exactMatch = _symbolTable.GetByFullName(query);
        if (exactMatch != null)
        {
            results.Add(new SearchResult { Symbol = exactMatch, Score = 1.0, MatchType = "exact" });
        }

        var nameMatches = _symbolTable.GetByName(query);
        foreach (var match in nameMatches)
        {
            if (!results.Any(r => r.Symbol.Id == match.Id))
            {
                results.Add(new SearchResult { Symbol = match, Score = 0.9, MatchType = "name" });
            }
        }

        var prefixMatches = FindByPrefix(query);
        foreach (var match in prefixMatches)
        {
            if (!results.Any(r => r.Symbol.Id == match.Id))
            {
                results.Add(new SearchResult { Symbol = match, Score = 0.7, MatchType = "prefix" });
            }
        }

        if (results.Count < options.MinResults)
        {
            var fuzzyMatches = FindByFuzzy(query);
            foreach (var match in fuzzyMatches)
            {
                if (!results.Any(r => r.Symbol.Id == match.Id))
                {
                    results.Add(new SearchResult { Symbol = match, Score = 0.4, MatchType = "fuzzy" });
                }
            }
        }

        if (options.KindFilter.HasValue)
        {
            results = results.Where(r => r.Symbol.Kind == options.KindFilter.Value).ToList();
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(options.MaxResults)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<ReferenceEntity> FindReferences(Guid symbolId, bool excludeDefinition = true)
    {
        return _symbolTable.GetReferences(symbolId);
    }

    public List<SourceCodeMatch> SearchSourceText(string query, int maxResults = 20)
    {
        var matches = new List<SourceCodeMatch>();
        if (string.IsNullOrWhiteSpace(query)) return matches;

        var filesByPath = new Dictionary<string, string[]>();
        foreach (var symbol in _symbolTable.GetByKind(SymbolKind.Type))
        {
            if (!string.IsNullOrEmpty(symbol.FilePath) && !filesByPath.ContainsKey(symbol.FilePath))
            {
                try
                {
                    if (File.Exists(symbol.FilePath))
                        filesByPath[symbol.FilePath] = File.ReadAllLines(symbol.FilePath);
                }
                catch { }
            }
        }

        foreach (var symbol in _symbolTable.GetByKind(SymbolKind.Method))
        {
            if (!string.IsNullOrEmpty(symbol.FilePath) && !filesByPath.ContainsKey(symbol.FilePath))
            {
                try
                {
                    if (File.Exists(symbol.FilePath))
                        filesByPath[symbol.FilePath] = File.ReadAllLines(symbol.FilePath);
                }
                catch { }
            }
        }

        foreach (var (filePath, lines) in filesByPath)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(new SourceCodeMatch
                    {
                        FilePath = filePath,
                        Line = i + 1,
                        Content = lines[i].Trim(),
                        Score = lines[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) == 0 ? 1.0 : 0.8
                    });
                }
            }
        }

        return matches.OrderByDescending(m => m.Score).Take(maxResults).ToList();
    }

    public IReadOnlyList<CallerInfo> FindCallers(Guid methodId, int maxDepth = 1)
    {
        var callers = new List<CallerInfo>();
        var visited = new HashSet<Guid>();

        FindCallersRecursive(methodId, maxDepth, 0, callers, visited);
        return callers.AsReadOnly();
    }

    public IReadOnlyList<CalleeInfo> FindCallees(Guid methodId, int maxDepth = 1)
    {
        var callees = new List<CalleeInfo>();
        var visited = new HashSet<Guid>();

        FindCalleesRecursive(methodId, maxDepth, 0, callees, visited);
        return callees.AsReadOnly();
    }

    public IReadOnlyList<SymbolEntity> FindImplementations(Guid interfaceId)
    {
        var implementations = new List<SymbolEntity>();
        var edges = _graph.GetIncomingEdges(interfaceId.ToByteArray());

        foreach (var edge in edges)
        {
            if (edge.Kind == EdgeKind.Implements)
            {
                var node = _graph.GetNode(edge.SourceId);
                if (node != null)
                {
                    var symbol = _symbolTable.GetByFullName(node.FullName);
                    if (symbol != null)
                    {
                        implementations.Add(symbol);
                    }
                }
            }
        }

        return implementations.AsReadOnly();
    }

    public IReadOnlyList<SymbolEntity> FindDerivedTypes(Guid typeId)
    {
        var derived = new List<SymbolEntity>();
        var visited = new HashSet<byte[]>();

        FindDerivedTypesRecursive(typeId.ToByteArray(), derived, visited);
        return derived.AsReadOnly();
    }

    public IReadOnlyList<SymbolEntity> FindOverrides(Guid methodId)
    {
        var overrides = new List<SymbolEntity>();
        var edges = _graph.GetIncomingEdges(methodId.ToByteArray());

        foreach (var edge in edges)
        {
            if (edge.Kind == EdgeKind.Overrides)
            {
                var node = _graph.GetNode(edge.SourceId);
                if (node != null)
                {
                    var symbol = _symbolTable.GetByFullName(node.FullName);
                    if (symbol != null)
                    {
                        overrides.Add(symbol);
                    }
                }
            }
        }

        return overrides.AsReadOnly();
    }

    private void BuildTrigramIndex()
    {
        var allSymbols = _symbolTable.GetByKind(SymbolKind.Type)
            .Concat(_symbolTable.GetByKind(SymbolKind.Method))
            .Concat(_symbolTable.GetByKind(SymbolKind.Property))
            .Concat(_symbolTable.GetByKind(SymbolKind.Field))
            .ToList();

        foreach (var symbol in allSymbols)
        {
            var trigrams = GenerateTrigrams(symbol.Name.ToLower());
            foreach (var trigram in trigrams)
            {
                _trigramIndex.AddOrUpdate(
                    trigram,
                    _ => new List<Guid> { symbol.Id },
                    (_, list) =>
                    {
                        lock (list)
                        {
                            if (!list.Contains(symbol.Id))
                                list.Add(symbol.Id);
                        }
                        return list;
                    });
            }
        }
    }

    private List<SymbolEntity> FindByPrefix(string prefix)
    {
        var results = new List<SymbolEntity>();
        var lowerPrefix = prefix.ToLower();

        var allSymbols = _symbolTable.GetByKind(SymbolKind.Type)
            .Concat(_symbolTable.GetByKind(SymbolKind.Method))
            .Concat(_symbolTable.GetByKind(SymbolKind.Property))
            .Concat(_symbolTable.GetByKind(SymbolKind.Field))
            .ToList();

        foreach (var symbol in allSymbols)
        {
            if (symbol.Name.StartsWith(lowerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(symbol);
            }
        }

        return results;
    }

    private List<SymbolEntity> FindByFuzzy(string query)
    {
        var queryTrigrams = GenerateTrigrams(query.ToLower());
        var candidateScores = new Dictionary<Guid, int>();

        foreach (var trigram in queryTrigrams)
        {
            if (_trigramIndex.TryGetValue(trigram, out var candidates))
            {
                foreach (var candidateId in candidates)
                {
                    candidateScores.TryGetValue(candidateId, out var count);
                    candidateScores[candidateId] = count + 1;
                }
            }
        }

        return candidateScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(20)
            .Select(kvp => _symbolTable.GetById(kvp.Key))
            .Where(s => s != null)
            .ToList()!;
    }

    private void FindCallersRecursive(Guid methodId, int maxDepth, int currentDepth, List<CallerInfo> callers, HashSet<Guid> visited)
    {
        if (currentDepth >= maxDepth || visited.Contains(methodId))
            return;

        visited.Add(methodId);

        var edges = _graph.GetIncomingEdges(methodId.ToByteArray());
        foreach (var edge in edges)
        {
            if (edge.Kind == EdgeKind.Calls)
            {
                var node = _graph.GetNode(edge.SourceId);
                if (node != null)
                {
                    var symbol = _symbolTable.GetByFullName(node.FullName);
                    if (symbol != null && !callers.Any(c => c.Symbol.Id == symbol.Id))
                    {
                        callers.Add(new CallerInfo
                        {
                            Symbol = symbol,
                            Depth = currentDepth + 1
                        });

                        FindCallersRecursive(symbol.Id, maxDepth, currentDepth + 1, callers, visited);
                    }
                }
            }
        }
    }

    private void FindCalleesRecursive(Guid methodId, int maxDepth, int currentDepth, List<CalleeInfo> callees, HashSet<Guid> visited)
    {
        if (currentDepth >= maxDepth || visited.Contains(methodId))
            return;

        visited.Add(methodId);

        var edges = _graph.GetOutgoingEdges(methodId.ToByteArray());
        foreach (var edge in edges)
        {
            if (edge.Kind == EdgeKind.Calls)
            {
                var node = _graph.GetNode(edge.TargetId);
                if (node != null)
                {
                    var symbol = _symbolTable.GetByFullName(node.FullName);
                    if (symbol != null && !callees.Any(c => c.Symbol.Id == symbol.Id))
                    {
                        callees.Add(new CalleeInfo
                        {
                            Symbol = symbol,
                            Depth = currentDepth + 1
                        });

                        FindCalleesRecursive(symbol.Id, maxDepth, currentDepth + 1, callees, visited);
                    }
                }
            }
        }
    }

    private void FindDerivedTypesRecursive(byte[] typeId, List<SymbolEntity> derived, HashSet<byte[]> visited)
    {
        if (visited.Any(v => v.SequenceEqual(typeId)))
            return;

        visited.Add(typeId);

        var edges = _graph.GetIncomingEdges(typeId);
        foreach (var edge in edges)
        {
            if (edge.Kind == EdgeKind.Inherits)
            {
                var node = _graph.GetNode(edge.SourceId);
                if (node != null)
                {
                    var symbol = _symbolTable.GetByFullName(node.FullName);
                    if (symbol != null && !derived.Any(d => d.Id == symbol.Id))
                    {
                        derived.Add(symbol);
                        FindDerivedTypesRecursive(edge.SourceId, derived, visited);
                    }
                }
            }
        }
    }

    private static List<string> GenerateTrigrams(string input)
    {
        if (input.Length < 3)
            return [input];

        var trigrams = new List<string>();
        for (int i = 0; i <= input.Length - 3; i++)
        {
            trigrams.Add(input.Substring(i, 3));
        }
        return trigrams;
    }
}

public class SearchResult
{
    public SymbolEntity Symbol { get; set; } = new();
    public double Score { get; set; }
    public string MatchType { get; set; } = string.Empty;
}

public class SearchOptions
{
    public int MaxResults { get; set; } = 50;
    public int MinResults { get; set; } = 5;
    public SymbolKind? KindFilter { get; set; }
    public string? ProjectFilter { get; set; }
    public string? NamespaceFilter { get; set; }
}

public class CallerInfo
{
    public SymbolEntity Symbol { get; set; } = new();
    public int Depth { get; set; }
}

public class CalleeInfo
{
    public SymbolEntity Symbol { get; set; } = new();
    public int Depth { get; set; }
}

public class SourceCodeMatch
{
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
}
