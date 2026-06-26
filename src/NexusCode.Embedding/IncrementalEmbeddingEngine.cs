using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using NexusCode.Domain;

namespace NexusCode.Embedding;

public sealed class IncrementalEmbeddingEngine : IDisposable
{
    private readonly EmbeddingEngine _embeddingEngine;
    private readonly SqliteConnection _db;
    private readonly ConcurrentDictionary<string, string> _fileHashes = new();

    public IncrementalEmbeddingEngine(EmbeddingEngine embeddingEngine, string dbPath = "nexus_embeddings.db")
    {
        _embeddingEngine = embeddingEngine;
        _db = new SqliteConnection($"Data Source={dbPath}");
        _db.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS embedding_meta (
                file_path TEXT PRIMARY KEY,
                content_hash TEXT NOT NULL,
                last_modified INTEGER NOT NULL,
                chunk_count INTEGER DEFAULT 0,
                model TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS embeddings (
                id TEXT PRIMARY KEY,
                file_path TEXT NOT NULL,
                chunk_index INTEGER NOT NULL,
                content_hash TEXT NOT NULL,
                model TEXT NOT NULL,
                created_at INTEGER NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    public async Task<List<EmbeddingResult>> IndexFileIncrementalAsync(
        string filePath,
        string content,
        string model,
        Func<string, List<string>> chunker,
        CancellationToken ct = default)
    {
        var contentHash = ComputeHash(content);
        var lastModified = File.GetLastWriteTimeUtc(filePath).Ticks;

        var existing = GetEmbeddingMeta(filePath);
        if (existing != null && existing.ContentHash == contentHash)
        {
            return GetCachedEmbeddings(filePath);
        }

        var chunks = chunker(content);
        var results = new List<EmbeddingResult>();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunkHash = ComputeHash(chunks[i]);
            var embedding = await _embeddingEngine.GetEmbeddingAsync(chunks[i], ct);

            if (embedding != null)
            {
                var result = new EmbeddingResult
                {
                    Id = Guid.NewGuid().ToString(),
                    FilePath = filePath,
                    ChunkIndex = i,
                    ContentHash = chunkHash,
                    Vector = embedding,
                    Model = model,
                    CreatedAt = DateTime.UtcNow
                };

                results.Add(result);
                SaveEmbedding(result);
            }
        }

        SaveEmbeddingMeta(filePath, contentHash, lastModified, chunks.Count, model);

        return results;
    }

    public async Task<List<EmbeddingResult>> EmbedBatchIncrementalAsync(
        List<(string Path, string Content)> files,
        string model,
        Func<string, List<string>> chunker,
        CancellationToken ct = default)
    {
        var allResults = new List<EmbeddingResult>();

        foreach (var (filePath, content) in files)
        {
            ct.ThrowIfCancellationRequested();

            var results = await IndexFileIncrementalAsync(filePath, content, model, chunker, ct);
            allResults.AddRange(results);
        }

        return allResults;
    }

    private EmbeddingMeta? GetEmbeddingMeta(string filePath)
    {
        var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT * FROM embedding_meta WHERE file_path = $path";
        cmd.Parameters.AddWithValue("$path", filePath);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new EmbeddingMeta
            {
                FilePath = reader.GetString(0),
                ContentHash = reader.GetString(1),
                LastModified = reader.GetInt64(2),
                ChunkCount = reader.GetInt32(3),
                Model = reader.GetString(4)
            };
        }
        return null;
    }

    private void SaveEmbeddingMeta(string filePath, string contentHash, long lastModified, int chunkCount, string model)
    {
        var cmd = _db.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO embedding_meta (file_path, content_hash, last_modified, chunk_count, model)
            VALUES ($path, $hash, $modified, $count, $model)";
        cmd.Parameters.AddWithValue("$path", filePath);
        cmd.Parameters.AddWithValue("$hash", contentHash);
        cmd.Parameters.AddWithValue("$modified", lastModified);
        cmd.Parameters.AddWithValue("$count", chunkCount);
        cmd.Parameters.AddWithValue("$model", model);
        cmd.ExecuteNonQuery();
    }

    private void SaveEmbedding(EmbeddingResult result)
    {
        var cmd = _db.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO embeddings (id, file_path, chunk_index, content_hash, model, created_at)
            VALUES ($id, $path, $index, $hash, $model, $created)";
        cmd.Parameters.AddWithValue("$id", result.Id);
        cmd.Parameters.AddWithValue("$path", result.FilePath);
        cmd.Parameters.AddWithValue("$index", result.ChunkIndex);
        cmd.Parameters.AddWithValue("$hash", result.ContentHash);
        cmd.Parameters.AddWithValue("$model", result.Model);
        cmd.Parameters.AddWithValue("$created", result.CreatedAt.Ticks);
        cmd.ExecuteNonQuery();
    }

    private List<EmbeddingResult> GetCachedEmbeddings(string filePath)
    {
        var results = new List<EmbeddingResult>();
        var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT id, file_path, chunk_index, content_hash, model, created_at FROM embeddings WHERE file_path = $path";
        cmd.Parameters.AddWithValue("$path", filePath);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new EmbeddingResult
            {
                Id = reader.GetString(0),
                FilePath = reader.GetString(1),
                ChunkIndex = reader.GetInt32(2),
                ContentHash = reader.GetString(3),
                Model = reader.GetString(4),
                CreatedAt = new DateTime(reader.GetInt64(5))
            });
        }
        return results;
    }

    private static string ComputeHash(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = System.Security.Cryptography.MD5.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

public class EmbeddingMeta
{
    public string FilePath { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public long LastModified { get; set; }
    public int ChunkCount { get; set; }
    public string Model { get; set; } = string.Empty;
}

public class EmbeddingResult
{
    public string Id { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public float[] Vector { get; set; } = [];
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
