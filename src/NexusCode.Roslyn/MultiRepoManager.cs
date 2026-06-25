using System.Collections.Concurrent;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class MultiRepoManager : IDisposable
{
    private readonly ConcurrentDictionary<string, RepoIndex> _repositories = new();
    private readonly RoslynEngine _engine;
    private readonly FileScanner _scanner;

    public int RepoCount => _repositories.Count;

    public MultiRepoManager()
    {
        _engine = new RoslynEngine();
        _scanner = new FileScanner();
    }

    public async Task<IndexResult> IndexRepositoryAsync(string repositoryPath, CancellationToken ct = default)
    {
        var repoName = Path.GetFileName(repositoryPath);
        var startTime = DateTimeOffset.UtcNow;
        var result = new IndexResult();

        try
        {
            var symbolTable = new SymbolTable();
            var graph = new KnowledgeGraph();

            var scanResult = await _scanner.ScanAsync(repositoryPath, new ScanOptions(), ct);

            foreach (var sln in scanResult.SolutionFiles)
            {
                try
                {
                    var solution = await _engine.LoadSolutionAsync(sln, ct);
                    foreach (var proj in solution.ProjectPaths)
                    {
                        try
                        {
                            await _engine.LoadProjectAsync(proj, ct);
                            _engine.BuildCompilation(proj, []);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            foreach (var csproj in scanResult.ProjectFiles)
            {
                if (_engine.GetCompilation(csproj) != null) continue;
                try
                {
                    await _engine.LoadProjectAsync(csproj, ct);
                    _engine.BuildCompilation(csproj, []);
                }
                catch { }
            }

            var compilations = _engine.GetAllCompilations();
            var processed = 0;

            await Parallel.ForEachAsync(scanResult.SourceFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (file, token) =>
            {
                try
                {
                    var comp = compilations.FirstOrDefault(c =>
                        c.SyntaxTrees.Any(t => string.Equals(t.FilePath, file, StringComparison.OrdinalIgnoreCase)));
                    if (comp == null) return;

                    var analysis = _engine.AnalyzeFile(file, comp);

                    lock (symbolTable)
                    {
                        foreach (var s in analysis.Symbols) symbolTable.Add(s);
                        foreach (var r in analysis.References) symbolTable.AddReference(r);
                        foreach (var n in analysis.GraphNodes) graph.AddNode(n);
                        foreach (var e in analysis.GraphEdges) graph.AddEdge(e);
                    }

                    Interlocked.Increment(ref processed);
                }
                catch { Interlocked.Increment(ref processed); }
            });

            var repoIndex = new RepoIndex
            {
                Name = repoName,
                Path = repositoryPath,
                SymbolTable = symbolTable,
                Graph = graph,
                IndexedAt = DateTimeOffset.UtcNow,
                FileCount = processed,
                SymbolCount = symbolTable.Count,
                NodeCount = graph.NodeCount,
                EdgeCount = graph.EdgeCount
            };

            _repositories[repoName] = repoIndex;

            result.Success = true;
            result.FilesIndexed = processed;
            result.SymbolsExtracted = symbolTable.Count;
            result.GraphNodesCreated = graph.NodeCount;
            result.GraphEdgesCreated = graph.EdgeCount;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        result.Duration = DateTimeOffset.UtcNow - startTime;
        return result;
    }

    public async Task<List<IndexResult>> IndexMultipleAsync(string[] repositoryPaths, CancellationToken ct = default)
    {
        var results = new List<IndexResult>();

        foreach (var path in repositoryPaths)
        {
            var result = await IndexRepositoryAsync(path, ct);
            results.Add(result);
        }

        return results;
    }

    public RepoIndex? GetRepository(string name)
    {
        return _repositories.TryGetValue(name, out var repo) ? repo : null;
    }

    public List<RepoIndex> GetAllRepositories()
    {
        return _repositories.Values.ToList();
    }

    public void AddRepository(RepoIndex repo)
    {
        _repositories[repo.Name] = repo;
    }

    public void RemoveRepository(string name)
    {
        _repositories.TryRemove(name, out _);
    }

    public SymbolSearchEngine? GetSearchEngine(string repoName)
    {
        var repo = GetRepository(repoName);
        return repo != null ? new SymbolSearchEngine(repo.SymbolTable, repo.Graph) : null;
    }

    public void Dispose()
    {
        _engine.Dispose();
        _repositories.Clear();
    }
}

public class RepoIndex
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public SymbolTable SymbolTable { get; set; } = new();
    public KnowledgeGraph Graph { get; set; } = new();
    public DateTimeOffset IndexedAt { get; set; }
    public int FileCount { get; set; }
    public int SymbolCount { get; set; }
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
}
