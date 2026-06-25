using Microsoft.CodeAnalysis;
using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Api.Services;

public sealed class NexusIndexService
{
    private readonly RoslynEngine _engine;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;
    private readonly SymbolSearchEngine _searchEngine;
    private readonly FileScanner _scanner;
    private bool _indexed;

    public NexusIndexService()
    {
        _engine = new RoslynEngine();
        _symbolTable = new SymbolTable();
        _graph = new KnowledgeGraph();
        _searchEngine = new SymbolSearchEngine(_symbolTable, _graph);
        _scanner = new FileScanner();
    }

    public SymbolSearchEngine SearchEngine => _searchEngine;
    public SymbolTable SymbolTable => _symbolTable;
    public KnowledgeGraph Graph => _graph;
    public bool IsIndexed => _indexed;

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
