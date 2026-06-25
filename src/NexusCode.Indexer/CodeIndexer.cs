using Microsoft.CodeAnalysis;
using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Indexer;

public sealed class CodeIndexer : IIndexer, IDisposable
{
    private readonly RoslynEngine _engine;
    private readonly FileScanner _scanner;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;

    public CodeIndexer()
    {
        _engine = new RoslynEngine();
        _scanner = new FileScanner();
        _symbolTable = new SymbolTable();
        _graph = new KnowledgeGraph();
    }

    public RoslynEngine Engine => _engine;
    public SymbolTable SymbolTable => _symbolTable;
    public KnowledgeGraph Graph => _graph;

    public async Task<IndexResult> IndexAsync(string repositoryPath, IndexOptions options, IProgress<IndexProgress>? progress = default, CancellationToken ct = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var result = new IndexResult();

        try
        {
            progress?.Report(new IndexProgress
            {
                Status = "Scanning files...",
                TotalFiles = 0,
                ProcessedFiles = 0
            });

            var scanResult = await _scanner.ScanAsync(repositoryPath, new ScanOptions(), ct);

            progress?.Report(new IndexProgress
            {
                Status = "Loading projects...",
                TotalFiles = scanResult.TotalFiles,
                ProcessedFiles = 0
            });

            foreach (var slnFile in scanResult.SolutionFiles)
            {
                ct.ThrowIfCancellationRequested();

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
                            Console.Error.WriteLine($"[Indexer] Failed to load project {projectPath}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Indexer] Failed to load solution {slnFile}: {ex.Message}");
                }
            }

            foreach (var csprojFile in scanResult.ProjectFiles)
            {
                ct.ThrowIfCancellationRequested();

                if (_engine.GetCompilation(csprojFile) != null) continue;

                try
                {
                    await _engine.LoadProjectAsync(csprojFile, ct);
                    _engine.BuildCompilation(csprojFile, []);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Indexer] Failed to load project {csprojFile}: {ex.Message}");
                }
            }

            var compilations = _engine.GetAllCompilations();
            var processedFiles = 0;
            var totalFiles = scanResult.SourceFiles.Count;

            progress?.Report(new IndexProgress
            {
                Status = "Analyzing files...",
                TotalFiles = totalFiles,
                ProcessedFiles = 0
            });

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxParallelism,
                CancellationToken = ct
            };

            var syncLock = new object();

            await Parallel.ForEachAsync(scanResult.SourceFiles, parallelOptions, async (filePath, token) =>
            {
                try
                {
                    var compilation = FindCompilation(filePath, compilations);
                    if (compilation == null)
                    {
                        Console.Error.WriteLine($"[Indexer] No compilation found for {filePath}");
                        return;
                    }

                    var analysisResult = _engine.AnalyzeFile(filePath, compilation);

                    lock (syncLock)
                    {
                        foreach (var symbol in analysisResult.Symbols)
                        {
                            _symbolTable.Add(symbol);
                        }

                        foreach (var reference in analysisResult.References)
                        {
                            _symbolTable.AddReference(reference);
                        }

                        foreach (var node in analysisResult.GraphNodes)
                        {
                            _graph.AddNode(node);
                        }

                        foreach (var edge in analysisResult.GraphEdges)
                        {
                            _graph.AddEdge(edge);
                        }
                    }

                    Interlocked.Increment(ref processedFiles);

                    if (processedFiles % 100 == 0 || processedFiles == totalFiles)
                    {
                        progress?.Report(new IndexProgress
                        {
                            Status = $"Analyzing files... {processedFiles}/{totalFiles}",
                            TotalFiles = totalFiles,
                            ProcessedFiles = processedFiles,
                            Elapsed = DateTimeOffset.UtcNow - startTime
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Indexer] Failed to analyze {filePath}: {ex.Message}");
                    Interlocked.Increment(ref processedFiles);
                }
            });

            result.Success = true;
            result.FilesIndexed = processedFiles;
            result.SymbolsExtracted = _symbolTable.Count;
            result.GraphNodesCreated = _graph.NodeCount;
            result.GraphEdgesCreated = _graph.EdgeCount;
            result.Duration = DateTimeOffset.UtcNow - startTime;

            progress?.Report(new IndexProgress
            {
                Status = "Complete",
                TotalFiles = totalFiles,
                ProcessedFiles = processedFiles,
                Elapsed = result.Duration
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.Duration = DateTimeOffset.UtcNow - startTime;
        }

        return result;
    }

    public async Task<IndexResult> IndexFileAsync(string filePath, CancellationToken ct = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var result = new IndexResult();

        try
        {
            var compilation = _engine.GetCompilation(filePath);
            if (compilation == null)
            {
                var projectDir = FindProjectDirectory(filePath);
                if (projectDir != null)
                {
                    var csprojFiles = Directory.GetFiles(projectDir, "*.csproj", SearchOption.TopDirectoryOnly);
                    foreach (var csproj in csprojFiles)
                    {
                        await _engine.LoadProjectAsync(csproj, ct);
                        _engine.BuildCompilation(csproj, []);
                        compilation = _engine.GetCompilation(csproj);
                        if (compilation != null) break;
                    }
                }
            }

            if (compilation == null)
            {
                result.Error = $"No compilation found for {filePath}";
                return result;
            }

            var analysisResult = _engine.AnalyzeFile(filePath, compilation);

            foreach (var symbol in analysisResult.Symbols)
            {
                _symbolTable.Add(symbol);
            }

            foreach (var reference in analysisResult.References)
            {
                _symbolTable.AddReference(reference);
            }

            foreach (var node in analysisResult.GraphNodes)
            {
                _graph.AddNode(node);
            }

            foreach (var edge in analysisResult.GraphEdges)
            {
                _graph.AddEdge(edge);
            }

            result.Success = true;
            result.FilesIndexed = 1;
            result.SymbolsExtracted = analysisResult.Symbols.Count;
            result.GraphNodesCreated = analysisResult.GraphNodes.Count;
            result.GraphEdgesCreated = analysisResult.GraphEdges.Count;
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
                {
                    return compilation;
                }
            }
        }

        return compilations.FirstOrDefault();
    }

    private string? FindProjectDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        while (directory != null)
        {
            if (Directory.GetFiles(directory, "*.csproj").Length > 0)
                return directory;

            directory = Path.GetDirectoryName(directory);
        }
        return null;
    }

    public void Dispose()
    {
        _engine.Dispose();
    }
}
