using System.Collections.Concurrent;

namespace NexusCode.VectorStore;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, VectorPoint>> _collections = new();

    public Task CreateCollectionAsync(string name, int dimension, CancellationToken ct = default)
    {
        _collections.TryAdd(name, new ConcurrentDictionary<string, VectorPoint>());
        return Task.CompletedTask;
    }

    public Task DeleteCollectionAsync(string name, CancellationToken ct = default)
    {
        _collections.TryRemove(name, out _);
        return Task.CompletedTask;
    }

    public Task<List<string>> ListCollectionsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_collections.Keys.ToList());
    }

    public async Task UpsertAsync(string collection, VectorPoint point, CancellationToken ct = default)
    {
        if (!_collections.ContainsKey(collection))
            await CreateCollectionAsync(collection, point.Vector.Length, ct);

        _collections[collection][point.Id] = point;
    }

    public async Task UpsertBatchAsync(string collection, List<VectorPoint> points, CancellationToken ct = default)
    {
        foreach (var point in points)
            await UpsertAsync(collection, point, ct);
    }

    public Task DeleteAsync(string collection, string id, CancellationToken ct = default)
    {
        if (_collections.TryGetValue(collection, out var coll))
            coll.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<List<SearchResult>> SearchAsync(string collection, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null, CancellationToken ct = default)
    {
        if (!_collections.TryGetValue(collection, out var coll))
            return Task.FromResult(new List<SearchResult>());

        var results = coll.Values
            .Select(p => new SearchResult
            {
                Id = p.Id,
                Score = CosineSimilarity(queryVector, p.Vector),
                Payload = p.Payload
            })
            .OrderByDescending(r => r.Score)
            .Take(limit)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<List<VectorPoint>> ScrollAsync(string collection, int limit = 100, string? offset = null, CancellationToken ct = default)
    {
        if (!_collections.TryGetValue(collection, out var coll))
            return Task.FromResult(new List<VectorPoint>());

        var points = coll.Values.Take(limit).ToList();
        return Task.FromResult(points);
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return normA == 0 || normB == 0 ? 0 : dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}
