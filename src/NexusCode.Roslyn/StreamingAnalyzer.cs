using Microsoft.CodeAnalysis;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class StreamingAnalyzer
{
    private readonly FileScanner _scanner;

    public StreamingAnalyzer()
    {
        _scanner = new FileScanner();
    }

    public async IAsyncEnumerable<AnalysisResult> AnalyzeStream(
        string repositoryPath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var scanResult = await _scanner.ScanAsync(repositoryPath, new ScanOptions(), ct);

        var engine = new RoslynEngine();
        foreach (var sln in scanResult.SolutionFiles)
        {
            try { await engine.LoadSolutionAsync(sln, ct); } catch { }
        }
        foreach (var csproj in scanResult.ProjectFiles)
        {
            try { await engine.LoadProjectAsync(csproj, ct); engine.BuildCompilation(csproj, []); } catch { }
        }

        var compilations = engine.GetAllCompilations();

        foreach (var file in scanResult.SourceFiles)
        {
            ct.ThrowIfCancellationRequested();

            var comp = compilations.FirstOrDefault(c =>
                c.SyntaxTrees.Any(t => string.Equals(t.FilePath, file, StringComparison.OrdinalIgnoreCase)));

            if (comp == null) continue;

            AnalysisResult? result = null;
            try
            {
                result = engine.AnalyzeFile(file, comp);
            }
            catch { }

            if (result != null)
                yield return result;
        }

        engine.Dispose();
    }

    public async Task<AnalysisResult> AnalyzeFileStreamingAsync(string filePath, Compilation compilation)
    {
        return await Task.Run(() =>
        {
            var engine = new RoslynEngine();
            var result = engine.AnalyzeFile(filePath, compilation);
            engine.Dispose();
            return result;
        });
    }
}
