using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class CrossRepoSearchEngine
{
    private readonly MultiRepoManager _manager;

    public CrossRepoSearchEngine(MultiRepoManager manager)
    {
        _manager = manager;
    }

    public List<CrossRepoSearchResult> Search(string query, int maxResults = 20)
    {
        var results = new List<CrossRepoSearchResult>();
        var repos = _manager.GetAllRepositories();

        foreach (var repo in repos)
        {
            var searchEngine = new SymbolSearchEngine(repo.SymbolTable, repo.Graph);
            var matches = searchEngine.FindSymbol(query, new SearchOptions { MaxResults = maxResults });

            foreach (var match in matches)
            {
                results.Add(new CrossRepoSearchResult
                {
                    Symbol = match.Symbol,
                    Score = match.Score,
                    Repository = repo.Name
                });
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(maxResults)
            .ToList();
    }

    public List<CrossRepoSearchResult> FindCallers(string symbolFullName, int maxDepth = 1)
    {
        var results = new List<CrossRepoSearchResult>();
        var repos = _manager.GetAllRepositories();

        foreach (var repo in repos)
        {
            var symbol = repo.SymbolTable.GetByFullName(symbolFullName);
            if (symbol == null) continue;

            var searchEngine = new SymbolSearchEngine(repo.SymbolTable, repo.Graph);
            var callers = searchEngine.FindCallers(symbol.Id, maxDepth);

            foreach (var caller in callers)
            {
                results.Add(new CrossRepoSearchResult
                {
                    Symbol = caller.Symbol,
                    Score = 1.0 - (caller.Depth * 0.2),
                    Repository = repo.Name,
                    Relation = "calls"
                });
            }
        }

        return results.OrderByDescending(r => r.Score).ToList();
    }

    public List<CrossRepoSearchResult> FindCallees(string symbolFullName, int maxDepth = 1)
    {
        var results = new List<CrossRepoSearchResult>();
        var repos = _manager.GetAllRepositories();

        foreach (var repo in repos)
        {
            var symbol = repo.SymbolTable.GetByFullName(symbolFullName);
            if (symbol == null) continue;

            var searchEngine = new SymbolSearchEngine(repo.SymbolTable, repo.Graph);
            var callees = searchEngine.FindCallees(symbol.Id, maxDepth);

            foreach (var callee in callees)
            {
                results.Add(new CrossRepoSearchResult
                {
                    Symbol = callee.Symbol,
                    Score = 1.0 - (callee.Depth * 0.2),
                    Repository = repo.Name,
                    Relation = "called by"
                });
            }
        }

        return results.OrderByDescending(r => r.Score).ToList();
    }

    public List<CrossRepoSearchResult> FindImplementations(string interfaceFullName)
    {
        var results = new List<CrossRepoSearchResult>();
        var repos = _manager.GetAllRepositories();

        foreach (var repo in repos)
        {
            var iface = repo.SymbolTable.GetByFullName(interfaceFullName);
            if (iface == null) continue;

            var searchEngine = new SymbolSearchEngine(repo.SymbolTable, repo.Graph);
            var implementations = searchEngine.FindImplementations(iface.Id);

            foreach (var impl in implementations)
            {
                results.Add(new CrossRepoSearchResult
                {
                    Symbol = impl,
                    Score = 1.0,
                    Repository = repo.Name,
                    Relation = "implements"
                });
            }
        }

        return results.ToList();
    }

    public List<CrossRepoSearchResult> FindDerivedTypes(string typeFullName)
    {
        var results = new List<CrossRepoSearchResult>();
        var repos = _manager.GetAllRepositories();

        foreach (var repo in repos)
        {
            var type = repo.SymbolTable.GetByFullName(typeFullName);
            if (type == null) continue;

            var searchEngine = new SymbolSearchEngine(repo.SymbolTable, repo.Graph);
            var derived = searchEngine.FindDerivedTypes(type.Id);

            foreach (var d in derived)
            {
                results.Add(new CrossRepoSearchResult
                {
                    Symbol = d,
                    Score = 1.0,
                    Repository = repo.Name,
                    Relation = "derives from"
                });
            }
        }

        return results.ToList();
    }

    public int FindSymbolInMultipleRepos(string symbolFullName)
    {
        int count = 0;
        var repos = _manager.GetAllRepositories();

        foreach (var repo in repos)
        {
            if (repo.SymbolTable.GetByFullName(symbolFullName) != null)
                count++;
        }

        return count;
    }
}

public class CrossRepoSearchResult
{
    public SymbolEntity Symbol { get; set; } = new();
    public double Score { get; set; }
    public string Repository { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;
}
