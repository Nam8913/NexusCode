using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class RepoComparator
{
    private readonly MultiRepoManager _manager;

    public RepoComparator(MultiRepoManager manager)
    {
        _manager = manager;
    }

    public ComparisonResult Compare(string repoName1, string repoName2)
    {
        var repo1 = _manager.GetRepository(repoName1);
        var repo2 = _manager.GetRepository(repoName2);

        if (repo1 == null || repo2 == null)
        {
            return new ComparisonResult { Error = "One or both repositories not found" };
        }

        var result = new ComparisonResult
        {
            Repo1 = repoName1,
            Repo2 = repoName2
        };

        var symbols1 = GetAllSymbols(repo1.SymbolTable);
        var symbols2 = GetAllSymbols(repo2.SymbolTable);

        var names1 = symbols1.Select(s => s.FullName).ToHashSet();
        var names2 = symbols2.Select(s => s.FullName).ToHashSet();

        result.CommonSymbols = names1.Intersect(names2).Count();
        result.UniqueToRepo1 = names1.Except(names2).Count();
        result.UniqueToRepo2 = names2.Except(names1).Count();

        foreach (var name in names1.Intersect(names2))
        {
            var s1 = symbols1.First(s => s.FullName == name);
            var s2 = symbols2.First(s => s.FullName == name);

            result.SymbolDiffs.Add(new SymbolDiff
            {
                FullName = name,
                InRepo1 = true,
                InRepo2 = true,
                Repo1Kind = s1.Kind.ToString(),
                Repo2Kind = s2.Kind.ToString(),
                AreSameKind = s1.Kind == s2.Kind
            });
        }

        foreach (var name in names1.Except(names2))
        {
            var s1 = symbols1.First(s => s.FullName == name);
            result.SymbolDiffs.Add(new SymbolDiff
            {
                FullName = name,
                InRepo1 = true,
                InRepo2 = false,
                Repo1Kind = s1.Kind.ToString()
            });
        }

        foreach (var name in names2.Except(names1))
        {
            var s2 = symbols2.First(s => s.FullName == name);
            result.SymbolDiffs.Add(new SymbolDiff
            {
                FullName = name,
                InRepo1 = false,
                InRepo2 = true,
                Repo2Kind = s2.Kind.ToString()
            });
        }

        result.Repo1Stats = GetRepoStats(repo1);
        result.Repo2Stats = GetRepoStats(repo2);

        return result;
    }

    private List<SymbolEntity> GetAllSymbols(SymbolTable table)
    {
        var symbols = new List<SymbolEntity>();
        foreach (var kind in Enum.GetValues<Domain.SymbolKind>())
        {
            symbols.AddRange(table.GetByKind(kind));
        }
        return symbols;
    }

    private RepoStats GetRepoStats(RepoIndex repo)
    {
        return new RepoStats
        {
            SymbolCount = repo.SymbolCount,
            NodeCount = repo.NodeCount,
            EdgeCount = repo.EdgeCount,
            ClassCount = repo.SymbolTable.GetByKind(Domain.SymbolKind.Type).Count(s => s.TypeName == "Class"),
            MethodCount = repo.SymbolTable.GetByKind(Domain.SymbolKind.Method).Count,
            PropertyCount = repo.SymbolTable.GetByKind(Domain.SymbolKind.Property).Count,
            FieldCount = repo.SymbolTable.GetByKind(Domain.SymbolKind.Field).Count
        };
    }
}

public class ComparisonResult
{
    public string Repo1 { get; set; } = string.Empty;
    public string Repo2 { get; set; } = string.Empty;
    public int CommonSymbols { get; set; }
    public int UniqueToRepo1 { get; set; }
    public int UniqueToRepo2 { get; set; }
    public List<SymbolDiff> SymbolDiffs { get; set; } = [];
    public RepoStats? Repo1Stats { get; set; }
    public RepoStats? Repo2Stats { get; set; }
    public string? Error { get; set; }
}

public class SymbolDiff
{
    public string FullName { get; set; } = string.Empty;
    public bool InRepo1 { get; set; }
    public bool InRepo2 { get; set; }
    public string? Repo1Kind { get; set; }
    public string? Repo2Kind { get; set; }
    public bool AreSameKind { get; set; }
}

public class RepoStats
{
    public int SymbolCount { get; set; }
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
    public int ClassCount { get; set; }
    public int MethodCount { get; set; }
    public int PropertyCount { get; set; }
    public int FieldCount { get; set; }
}
