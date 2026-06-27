using NexusCode.Domain;
using NexusCode.Roslyn;
using NexusCode.Database;

namespace NexusCode.Api.Services;

public sealed class PersistenceService : IDisposable
{
    private readonly SqliteRepository _repository;

    private static string ConfigDir => Path.Combine(AppContext.BaseDirectory, ".nexus");
    private static string ConfigFile => Path.Combine(ConfigDir, "indexed_repos.json");

    public PersistenceService()
    {
        _repository = new SqliteRepository();
    }

    public bool HasPersistedData() => _repository.HasPersistedData();

    public void RestoreSymbols(SymbolTable symbolTable)
    {
        var symbols = _repository.LoadSymbols();
        foreach (var symbol in symbols)
            symbolTable.Add(symbol);
    }

    public void RestoreGraph(KnowledgeGraph graph)
    {
        _repository.LoadGraph(graph);
    }

    public void SaveSymbols(SymbolTable symbolTable)
    {
        _repository.SaveSymbols(symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Type)
            .Concat(symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Method))
            .Concat(symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Property))
            .Concat(symbolTable.GetByKind(NexusCode.Domain.SymbolKind.Field)));
    }

    public void SaveGraph(KnowledgeGraph graph)
    {
        _repository.SaveGraph(graph);
    }

    public List<string> LoadSavedPaths()
    {
        try
        {
            if (!File.Exists(ConfigFile)) return [];
            var json = File.ReadAllText(ConfigFile);
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Persistence] Failed to load paths: {ex.Message}");
            return [];
        }
    }

    public void SavePath(string path)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);

            var lockPath = ConfigFile + ".lock";
            using (var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                lockStream.Lock(0, 1);

                List<string> paths = [];
                if (File.Exists(ConfigFile))
                {
                    var json = File.ReadAllText(ConfigFile);
                    paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
                }
                if (!paths.Contains(path, StringComparer.OrdinalIgnoreCase))
                    paths.Add(path);
                File.WriteAllText(ConfigFile, System.Text.Json.JsonSerializer.Serialize(paths));

                lockStream.Unlock(0, 1);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Persistence] Failed to save path {path}: {ex.Message}");
        }
    }

    public void RemoveSavedPath(string repoName)
    {
        try
        {
            if (!File.Exists(ConfigFile)) return;

            var lockPath = ConfigFile + ".lock";
            using (var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                lockStream.Lock(0, 1);

                var json = File.ReadAllText(ConfigFile);
                var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
                paths.RemoveAll(p => Path.GetFileName(p).Equals(repoName, StringComparison.OrdinalIgnoreCase));
                File.WriteAllText(ConfigFile, System.Text.Json.JsonSerializer.Serialize(paths));

                lockStream.Unlock(0, 1);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Persistence] Failed to remove path {repoName}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _repository.Dispose();
    }
}
