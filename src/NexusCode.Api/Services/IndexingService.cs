using Microsoft.CodeAnalysis;
using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Api.Services;

public sealed class IndexingService
{
    private readonly RoslynEngine _engine;
    private readonly FileScanner _scanner;

    public IndexingService()
    {
        _engine = new RoslynEngine();
        _scanner = new FileScanner();
    }

    public async Task<IndexResult> IndexAsync(
        string repositoryPath,
        SymbolTable symbolTable,
        KnowledgeGraph graph,
        IProgress<IndexProgress>? progress = default,
        CancellationToken ct = default)
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
                        foreach (var symbol in analysisResult.Symbols) symbolTable.Add(symbol);
                        foreach (var reference in analysisResult.References) symbolTable.AddReference(reference);
                        foreach (var node in analysisResult.GraphNodes) graph.AddNode(node);
                        foreach (var edge in analysisResult.GraphEdges) graph.AddEdge(edge);
                    }

                    Interlocked.Increment(ref processedFiles);
                }
                catch { Interlocked.Increment(ref processedFiles); }
            });

            result.Success = true;
            result.FilesIndexed = processedFiles;
            result.SymbolsExtracted = symbolTable.Count;
            result.GraphNodesCreated = graph.NodeCount;
            result.GraphEdgesCreated = graph.EdgeCount;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        result.Duration = DateTimeOffset.UtcNow - startTime;
        return result;
    }

    public Compilation? FindCompilation(string filePath, IReadOnlyList<Compilation> compilations)
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
