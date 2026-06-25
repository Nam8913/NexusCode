using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class RepositoryWatcher : IDisposable
{
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, string> _fileHashes = new();
    private readonly RoslynEngine _engine;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;
    private readonly object _lock = new();
    private bool _isWatching;

    public event Action<FileChangeEventArgs>? FileChanged;
    public event Action<IndexProgress>? ProgressChanged;

    public bool IsWatching => _isWatching;

    public RepositoryWatcher(RoslynEngine engine, SymbolTable symbolTable, KnowledgeGraph graph)
    {
        _engine = engine;
        _symbolTable = symbolTable;
        _graph = graph;
    }

    public void StartWatching(string repositoryPath)
    {
        if (_isWatching) return;

        var options = new ScanOptions();
        ScanDirectory(repositoryPath, repositoryPath, options);

        _isWatching = true;
    }

    public void StopWatching()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        _isWatching = false;
    }

    public ChangeSet DetectChanges(string repositoryPath)
    {
        var changeSet = new ChangeSet();
        var currentFiles = new Dictionary<string, string>();

        var options = new ScanOptions();
        CollectFiles(repositoryPath, repositoryPath, options, currentFiles);

        foreach (var kvp in currentFiles)
        {
            if (!_fileHashes.ContainsKey(kvp.Key))
            {
                changeSet.NewFiles.Add(kvp.Key);
            }
            else if (_fileHashes[kvp.Key] != kvp.Value)
            {
                changeSet.ModifiedFiles.Add(kvp.Key);
            }
        }

        foreach (var trackedFile in _fileHashes.Keys)
        {
            if (!currentFiles.ContainsKey(trackedFile))
            {
                changeSet.DeletedFiles.Add(trackedFile);
            }
        }

        return changeSet;
    }

    public async Task<IndexResult> ApplyChangesAsync(ChangeSet changeSet, CancellationToken ct = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var result = new IndexResult();

        try
        {
            var compilations = _engine.GetAllCompilations();

            foreach (var file in changeSet.DeletedFiles)
            {
                RemoveFile(file);
                _fileHashes.TryRemove(file, out _);
            }

            foreach (var file in changeSet.ModifiedFiles)
            {
                await UpdateFileAsync(file, compilations, ct);
            }

            foreach (var file in changeSet.NewFiles)
            {
                await AddFileAsync(file, compilations, ct);
            }

            result.Success = true;
            result.FilesIndexed = changeSet.TotalChanges;
            result.SymbolsExtracted = _symbolTable.Count;
            result.GraphNodesCreated = _graph.NodeCount;
            result.GraphEdgesCreated = _graph.EdgeCount;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        result.Duration = DateTimeOffset.UtcNow - startTime;
        return result;
    }

    private void ScanDirectory(string rootPath, string currentPath, ScanOptions options)
    {
        try
        {
            foreach (var file in Directory.GetFiles(currentPath))
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (!options.IncludeExtensions.Contains(extension)) continue;
                if (IsExcluded(file, rootPath, options)) continue;

                var hash = ComputeHash(file);
                _fileHashes[file] = hash;

                var dir = Path.GetDirectoryName(file);
                if (dir != null && !_watchers.ContainsKey(dir))
                {
                    CreateWatcher(dir);
                }
            }

            foreach (var dir in Directory.GetDirectories(currentPath))
            {
                var dirName = Path.GetFileName(dir);
                if (options.ExcludePatterns.Contains(dirName, StringComparer.OrdinalIgnoreCase)) continue;
                ScanDirectory(rootPath, dir, options);
            }
        }
        catch { }
    }

    private void CollectFiles(string rootPath, string currentPath, ScanOptions options, Dictionary<string, string> files)
    {
        try
        {
            foreach (var file in Directory.GetFiles(currentPath))
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (!options.IncludeExtensions.Contains(extension)) continue;
                if (IsExcluded(file, rootPath, options)) continue;

                files[file] = ComputeHash(file);
            }

            foreach (var dir in Directory.GetDirectories(currentPath))
            {
                var dirName = Path.GetFileName(dir);
                if (options.ExcludePatterns.Contains(dirName, StringComparer.OrdinalIgnoreCase)) continue;
                CollectFiles(rootPath, dir, options, files);
            }
        }
        catch { }
    }

    private void CreateWatcher(string directory)
    {
        var watcher = new FileSystemWatcher(directory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileCreated;
        watcher.Deleted += OnFileDeleted;
        watcher.Renamed += OnFileRenamed;

        _watchers[directory] = watcher;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsCSharpFile(e.FullPath)) return;

        var newHash = ComputeHash(e.FullPath);
        var oldHash = _fileHashes.GetValueOrDefault(e.FullPath);

        if (newHash != oldHash)
        {
            _fileHashes[e.FullPath] = newHash;
            FileChanged?.Invoke(new FileChangeEventArgs
            {
                FilePath = e.FullPath,
                ChangeType = oldHash == null ? ChangeType.New : ChangeType.Modified
            });
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!IsCSharpFile(e.FullPath)) return;

        var hash = ComputeHash(e.FullPath);
        _fileHashes[e.FullPath] = hash;

        FileChanged?.Invoke(new FileChangeEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = ChangeType.New
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!IsCSharpFile(e.FullPath)) return;

        _fileHashes.TryRemove(e.FullPath, out _);

        FileChanged?.Invoke(new FileChangeEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = ChangeType.Deleted
        });
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!IsCSharpFile(e.OldFullPath)) return;

        _fileHashes.TryRemove(e.OldFullPath, out _);

        FileChanged?.Invoke(new FileChangeEventArgs
        {
            FilePath = e.OldFullPath,
            ChangeType = ChangeType.Deleted
        });

        var hash = ComputeHash(e.FullPath);
        _fileHashes[e.FullPath] = hash;

        FileChanged?.Invoke(new FileChangeEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = ChangeType.New
        });
    }

    private void RemoveFile(string filePath)
    {
        var symbols = _symbolTable.GetByFile(filePath);
        foreach (var symbol in symbols)
        {
            _symbolTable.Remove(symbol.Id);
        }

        var nodes = _graph.GetNodesByFile(filePath);
        foreach (var node in nodes)
        {
            _graph.RemoveNode(node.Id);
        }
    }

    private async Task UpdateFileAsync(string filePath, IReadOnlyList<Compilation> compilations, CancellationToken ct)
    {
        RemoveFile(filePath);
        await AddFileAsync(filePath, compilations, ct);
    }

    private async Task AddFileAsync(string filePath, IReadOnlyList<Compilation> compilations, CancellationToken ct)
    {
        var compilation = compilations.FirstOrDefault(c =>
            c.SyntaxTrees.Any(t => string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase)));

        if (compilation == null) return;

        var analysis = _engine.AnalyzeFile(filePath, compilation);

        foreach (var symbol in analysis.Symbols) _symbolTable.Add(symbol);
        foreach (var reference in analysis.References) _symbolTable.AddReference(reference);
        foreach (var node in analysis.GraphNodes) _graph.AddNode(node);
        foreach (var edge in analysis.GraphEdges) _graph.AddEdge(edge);

        _fileHashes[filePath] = ComputeHash(filePath);

        await Task.CompletedTask;
    }

    private static bool IsCSharpFile(string path)
    {
        return path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
               !path.Contains("bin") && !path.Contains("obj");
    }

    private static bool IsExcluded(string filePath, string rootPath, ScanOptions options)
    {
        var relativePath = Path.GetRelativePath(rootPath, filePath);
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => options.ExcludePatterns.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    private static string ComputeHash(string filePath)
    {
        try
        {
            var bytes = File.ReadAllBytes(filePath);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
        catch
        {
            return "";
        }
    }

    public void Dispose()
    {
        StopWatching();
    }
}

public class FileChangeEventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }
}
