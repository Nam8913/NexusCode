using System.Security.Cryptography;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class IncrementalIndexer
{
    private readonly RoslynEngine _engine;
    private readonly FileScanner _scanner;
    private readonly Dictionary<string, string> _fileHashes = new();

    public IncrementalIndexer()
    {
        _engine = new RoslynEngine();
        _scanner = new FileScanner();
    }

    public async Task<IndexResult> IndexIncrementalAsync(
        string repositoryPath,
        SymbolTable symbolTable,
        KnowledgeGraph graph,
        CancellationToken ct = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var result = new IndexResult();

        var checkpoint = await LoadCheckpointAsync(repositoryPath);

        var changeSet = await _scanner.DetectChangesAsync(repositoryPath, ct);

        if (!changeSet.HasChanges && checkpoint != null)
        {
            result.Success = true;
            result.FilesIndexed = 0;
            result.Duration = DateTimeOffset.UtcNow - startTime;
            return result;
        }

        var filesToIndex = new List<string>();
        filesToIndex.AddRange(changeSet.NewFiles);
        filesToIndex.AddRange(changeSet.ModifiedFiles);

        foreach (var file in changeSet.DeletedFiles)
        {
            RemoveFileFromAnalysis(file, symbolTable, graph);
        }

        if (filesToIndex.Count > 0)
        {
            var indexResult = await IndexFilesAsync(filesToIndex, symbolTable, graph, ct);
            result.FilesIndexed = indexResult;
        }

        await SaveCheckpointAsync(repositoryPath, new IndexCheckpoint
        {
            RepositoryPath = repositoryPath,
            FileHashes = new Dictionary<string, string>(_fileHashes),
            LastIndexed = DateTimeOffset.UtcNow
        });

        result.Success = true;
        result.SymbolsExtracted = symbolTable.Count;
        result.GraphNodesCreated = graph.NodeCount;
        result.GraphEdgesCreated = graph.EdgeCount;
        result.Duration = DateTimeOffset.UtcNow - startTime;

        return result;
    }

    private async Task<int> IndexFilesAsync(List<string> files, SymbolTable symbolTable, KnowledgeGraph graph, CancellationToken ct)
    {
        var processed = 0;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var compilation = _engine.GetCompilation(file) ??
                    _engine.GetAllCompilations().FirstOrDefault();

                if (compilation == null) continue;

                var analysis = _engine.AnalyzeFile(file, compilation);

                foreach (var symbol in analysis.Symbols) symbolTable.Add(symbol);
                foreach (var reference in analysis.References) symbolTable.AddReference(reference);
                foreach (var node in analysis.GraphNodes) graph.AddNode(node);
                foreach (var edge in analysis.GraphEdges) graph.AddEdge(edge);

                var hash = ComputeHash(file);
                _fileHashes[file] = hash;

                Interlocked.Increment(ref processed);
            }
            catch { }
        }

        return processed;
    }

    private void RemoveFileFromAnalysis(string file, SymbolTable symbolTable, KnowledgeGraph graph)
    {
        var symbols = symbolTable.GetByFile(file);
        foreach (var symbol in symbols)
            symbolTable.Remove(symbol.Id);

        var nodes = graph.GetNodesByFile(file);
        foreach (var node in nodes)
            graph.RemoveNode(node.Id);

        _fileHashes.Remove(file);
    }

    public async Task SaveCheckpointAsync(string repositoryPath, IndexCheckpoint checkpoint)
    {
        var dir = Path.Combine(repositoryPath, ".nexus");
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, "checkpoint.json");
        var json = System.Text.Json.JsonSerializer.Serialize(checkpoint);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<IndexCheckpoint?> LoadCheckpointAsync(string repositoryPath)
    {
        var path = Path.Combine(repositoryPath, ".nexus", "checkpoint.json");
        if (!File.Exists(path)) return null;

        var json = await File.ReadAllTextAsync(path);
        return System.Text.Json.JsonSerializer.Deserialize<IndexCheckpoint>(json);
    }

    private static string ComputeHash(string filePath)
    {
        try
        {
            var bytes = File.ReadAllBytes(filePath);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
        catch { return ""; }
    }
}

public class IndexCheckpoint
{
    public string RepositoryPath { get; set; } = string.Empty;
    public Dictionary<string, string> FileHashes { get; set; } = new();
    public DateTimeOffset LastIndexed { get; set; }
}
