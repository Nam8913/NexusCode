namespace NexusCode.VectorStore;

public interface IVectorStore
{
    Task CreateCollectionAsync(string name, int dimension, CancellationToken ct = default);
    Task DeleteCollectionAsync(string name, CancellationToken ct = default);
    Task<List<string>> ListCollectionsAsync(CancellationToken ct = default);
    Task UpsertAsync(string collection, VectorPoint point, CancellationToken ct = default);
    Task UpsertBatchAsync(string collection, List<VectorPoint> points, CancellationToken ct = default);
    Task DeleteAsync(string collection, string id, CancellationToken ct = default);
    Task<List<SearchResult>> SearchAsync(string collection, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null, CancellationToken ct = default);
    Task<List<VectorPoint>> ScrollAsync(string collection, int limit = 100, string? offset = null, CancellationToken ct = default);
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}

public class VectorPoint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public float[] Vector { get; set; } = [];
    public Dictionary<string, object> Payload { get; set; } = new();
}

public class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public float Score { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
}
