using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.UI.Services;

public sealed class NexusService
{
    private readonly RoslynEngine _engine;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;
    private readonly SymbolSearchEngine _searchEngine;
    private readonly FileScanner _scanner;
    private readonly ContextBuilder _contextBuilder;
    private readonly GraphRAGEngine _ragEngine;
    private bool _indexed;

    public NexusService()
    {
        _engine = new RoslynEngine();
        _symbolTable = new SymbolTable();
        _graph = new KnowledgeGraph();
        _searchEngine = new SymbolSearchEngine(_symbolTable, _graph);
        _scanner = new FileScanner();
        _contextBuilder = new ContextBuilder(_searchEngine, _symbolTable, _graph);
        _ragEngine = new GraphRAGEngine(_searchEngine, _symbolTable, _graph);
    }

    public bool IsIndexed => _indexed;
    public SymbolTable SymbolTable => _symbolTable;
    public KnowledgeGraph Graph => _graph;
    public SymbolSearchEngine SearchEngine => _searchEngine;

    public async Task<IndexResult> IndexRepositoryAsync(string path, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = new IndexResult();

        try
        {
            var scan = await _scanner.ScanAsync(path, new ScanOptions(), ct);

            foreach (var sln in scan.SolutionFiles)
            {
                try
                {
                    var sol = await _engine.LoadSolutionAsync(sln, ct);
                    foreach (var proj in sol.ProjectPaths)
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

            foreach (var csproj in scan.ProjectFiles)
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

            await Parallel.ForEachAsync(scan.SourceFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (file, token) =>
            {
                try
                {
                    var comp = compilations.FirstOrDefault(c => c.SyntaxTrees.Any(t => string.Equals(t.FilePath, file, StringComparison.OrdinalIgnoreCase)));
                    if (comp == null) return;

                    var analysis = _engine.AnalyzeFile(file, comp);

                    lock (_symbolTable)
                    {
                        foreach (var s in analysis.Symbols) _symbolTable.Add(s);
                        foreach (var r in analysis.References) _symbolTable.AddReference(r);
                        foreach (var n in analysis.GraphNodes) _graph.AddNode(n);
                        foreach (var e in analysis.GraphEdges) _graph.AddEdge(e);
                    }

                    Interlocked.Increment(ref processed);
                }
                catch { Interlocked.Increment(ref processed); }
            });

            _indexed = true;
            result.Success = true;
            result.FilesIndexed = processed;
            result.SymbolsExtracted = _symbolTable.Count;
            result.GraphNodesCreated = _graph.NodeCount;
            result.GraphEdgesCreated = _graph.EdgeCount;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    public ContextResult BuildContext(string question) => _contextBuilder.BuildContext(question);
    public string BuildPrompt(ContextResult ctx) => _contextBuilder.BuildPrompt(ctx);
    public GraphRAGResult GraphRAG(string question) => _ragEngine.Answer(question);
}
