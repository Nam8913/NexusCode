using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Api.Services;

public sealed class NexusIndexService : IDisposable
{
    private readonly IndexingService _indexingService;
    private readonly PersistenceService _persistenceService;
    private readonly MultiRepoManagerService _multiRepoService;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;
    private readonly SymbolSearchEngine _searchEngine;
    private readonly RepositoryWatcher _watcher;
    private bool _indexed;

    public NexusIndexService(
        IndexingService indexingService,
        PersistenceService persistenceService,
        MultiRepoManagerService multiRepoService)
    {
        _indexingService = indexingService;
        _persistenceService = persistenceService;
        _multiRepoService = multiRepoService;
        _symbolTable = new SymbolTable();
        _graph = new KnowledgeGraph();
        _searchEngine = new SymbolSearchEngine(_symbolTable, _graph);
        _watcher = new RepositoryWatcher(new RoslynEngine(), _symbolTable, _graph);

        RestoreFromPersistence();
    }

    private void RestoreFromPersistence()
    {
        try
        {
            if (_persistenceService.HasPersistedData())
            {
                _persistenceService.RestoreSymbols(_symbolTable);
                _persistenceService.RestoreGraph(_graph);

                if (_symbolTable.Count > 0)
                {
                    _indexed = true;
                    Console.WriteLine($"[NexusIndex] Restored {_symbolTable.Count} symbols, {_graph.NodeCount} graph nodes");

                    var savedPaths = _persistenceService.LoadSavedPaths();
                    foreach (var path in savedPaths)
                    {
                        if (!Directory.Exists(path)) continue;
                        var repoName = Path.GetFileName(path);
                        _multiRepoService.Manager.AddRepository(new RepoIndex
                        {
                            Name = repoName,
                            Path = path,
                            SymbolTable = _symbolTable,
                            Graph = _graph,
                            IndexedAt = File.GetLastWriteTimeUtc(path),
                            SymbolCount = _symbolTable.Count,
                            NodeCount = _graph.NodeCount,
                            EdgeCount = _graph.EdgeCount
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[NexusIndex] Failed to restore: {ex.Message}");
        }
    }

    public SymbolSearchEngine SearchEngine => _searchEngine;
    public SymbolTable SymbolTable => _symbolTable;
    public KnowledgeGraph Graph => _graph;
    public bool IsIndexed => _indexed;

    public GraphRAGResult GraphRAG(string question)
    {
        var ragEngine = new GraphRAGEngine(_searchEngine, _symbolTable, _graph);
        return ragEngine.Answer(question);
    }

    public async Task<IndexResult> IndexRepositoryAsync(string repositoryPath, IProgress<IndexProgress>? progress = default, CancellationToken ct = default)
    {
        var result = await _indexingService.IndexAsync(repositoryPath, _symbolTable, _graph, progress, ct);

        if (result.Success)
        {
            _indexed = true;
            _persistenceService.SavePath(repositoryPath);

            try
            {
                _persistenceService.SaveSymbols(_symbolTable);
                _persistenceService.SaveGraph(_graph);
                Console.WriteLine("[NexusIndex] Saved to SQLite");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NexusIndex] Failed to save to SQLite: {ex.Message}");
            }

            try
            {
                _watcher.StartWatching(repositoryPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NexusIndex] Failed to start watcher: {ex.Message}");
            }

            var repoName = Path.GetFileName(repositoryPath);
            _multiRepoService.Manager.AddRepository(new RepoIndex
            {
                Name = repoName,
                Path = repositoryPath,
                SymbolTable = _symbolTable,
                Graph = _graph,
                IndexedAt = DateTimeOffset.UtcNow,
                SymbolCount = _symbolTable.Count,
                NodeCount = _graph.NodeCount,
                EdgeCount = _graph.EdgeCount
            });
        }

        return result;
    }

    public void RemoveSavedPath(string repoName)
    {
        _persistenceService.RemoveSavedPath(repoName);
    }

    public void Dispose()
    {
        _persistenceService.Dispose();
    }
}
