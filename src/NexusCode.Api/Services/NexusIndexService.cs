using Microsoft.CodeAnalysis;
using NexusCode.Domain;
using NexusCode.Roslyn;
using NexusCode.Database;

namespace NexusCode.Api.Services;

public sealed class NexusIndexService
{
    private readonly RoslynEngine _engine;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;
    private readonly SymbolSearchEngine _searchEngine;
    private readonly FileScanner _scanner;
    private readonly SqliteRepository _repository;
    private readonly RepositoryWatcher _watcher;
    private bool _indexed;

    public NexusIndexService()
    {
        _engine = new RoslynEngine();
        _symbolTable = new SymbolTable();
        _graph = new KnowledgeGraph();
        _searchEngine = new SymbolSearchEngine(_symbolTable, _graph);
        _scanner = new FileScanner();
        _repository = new SqliteRepository();
        _watcher = new RepositoryWatcher(_engine, _symbolTable, _graph);

        RestoreFromDatabase();
    }

    private void RestoreFromDatabase()
    {
        try
        {
            if (_repository.HasPersistedData())
            {
                var symbols = _repository.LoadSymbols();
                foreach (var symbol in symbols)
                    _symbolTable.Add(symbol);

                _repository.LoadGraph(_graph);

                if (_symbolTable.Count > 0)
                {
                    _indexed = true;
                    Console.WriteLine($"[NexusIndex] Restored {_symbolTable.Count} symbols, {_graph.NodeCount} graph nodes from SQLite");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[NexusIndex] Failed to restore from database: {ex.Message}");
        }
    }

    public SymbolSearchEngine SearchEngine => _searchEngine;
    public SymbolTable SymbolTable => _symbolTable;
    public KnowledgeGraph Graph => _graph;
    public bool IsIndexed => _indexed;
    public bool IsWatching => _watcher.IsWatching;

    public void StartWatching(string repositoryPath)
    {
        _watcher.StartWatching(repositoryPath);
    }

    public void StopWatching()
    {
        _watcher.StopWatching();
    }

    public GraphRAGResult GraphRAG(string question)
    {
        var ragEngine = new GraphRAGEngine(_searchEngine, _symbolTable, _graph);
        return ragEngine.Answer(question);
    }

    public async Task<IndexResult> IndexRepositoryAsync(string repositoryPath, IProgress<IndexProgress>? progress = default, CancellationToken ct = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var result = new IndexResult();

        try
        {
            var scanResult = await _scanner.ScanAsync(repositoryPath, new ScanOptions(), ct);

            foreach (var slnFile in scanResult.SolutionFiles)
            {
                try
                {
                    var solutionInfo = await _engine.LoadSolutionAsync(slnFile, ct);
                    foreach (var projectPath in solutionInfo.ProjectPaths)
                    {
                        try
                        {
                            await _engine.LoadProjectAsync(projectPath, ct);
                            _engine.BuildCompilation(projectPath, []);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Failed to load project {projectPath}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to load solution {slnFile}: {ex.Message}");
                }
            }

            foreach (var csprojFile in scanResult.ProjectFiles)
            {
                if (_engine.GetCompilation(csprojFile) != null) continue;
                try
                {
                    await _engine.LoadProjectAsync(csprojFile, ct);
                    _engine.BuildCompilation(csprojFile, []);
                }
                catch { }
            }

            var compilations = _engine.GetAllCompilations();
            var processedFiles = 0;
            var totalFiles = scanResult.SourceFiles.Count;

            var syncLock = new object();

            await Parallel.ForEachAsync(scanResult.SourceFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (filePath, token) =>
            {
                try
                {
                    var compilation = FindCompilation(filePath, compilations);
                    if (compilation == null) return;

                    var analysisResult = _engine.AnalyzeFile(filePath, compilation);

                    lock (syncLock)
                    {
                        foreach (var symbol in analysisResult.Symbols) _symbolTable.Add(symbol);
                        foreach (var reference in analysisResult.References) _symbolTable.AddReference(reference);
                        foreach (var node in analysisResult.GraphNodes) _graph.AddNode(node);
                        foreach (var edge in analysisResult.GraphEdges) _graph.AddEdge(edge);
                    }

                    Interlocked.Increment(ref processedFiles);
                }
                catch { Interlocked.Increment(ref processedFiles); }
            });

            _indexed = true;

            try
            {
                _repository.SaveSymbols(_symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Type)
                    .Concat(_symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Method))
                    .Concat(_symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Property))
                    .Concat(_symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Field)));
                _repository.SaveGraph(_graph);
                Console.WriteLine("[NexusIndex] Saved to SQLite");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NexusIndex] Failed to save to SQLite: {ex.Message}");
            }

            try
            {
                _watcher.StartWatching(repositoryPath);
                Console.WriteLine("[NexusIndex] Started file watcher");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NexusIndex] Failed to start watcher: {ex.Message}");
            }

            result.Success = true;
            result.FilesIndexed = processedFiles;
            result.SymbolsExtracted = _symbolTable.Count;
            result.GraphNodesCreated = _graph.NodeCount;
            result.GraphEdgesCreated = _graph.EdgeCount;
            result.Duration = DateTimeOffset.UtcNow - startTime;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.Duration = DateTimeOffset.UtcNow - startTime;
        }

        return result;
    }

    private Compilation? FindCompilation(string filePath, IReadOnlyList<Compilation> compilations)
    {
        foreach (var compilation in compilations)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                if (string.Equals(tree.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                    return compilation;
            }
        }
        return compilations.FirstOrDefault();
    }
}
