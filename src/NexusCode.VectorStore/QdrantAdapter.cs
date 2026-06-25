using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusCode.VectorStore;

public sealed class QdrantAdapter : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public QdrantAdapter(string baseUrl = "http://localhost:6333")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task CreateCollectionAsync(string name, int dimension, CancellationToken ct = default)
    {
        var request = new
        {
            vectors = new { size = dimension, distance = "Cosine" }
        };
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/collections/{name}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteCollectionAsync(string name, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/collections/{name}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<string>> ListCollectionsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/collections", ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CollectionsResponse>(cancellationToken: ct);
        return result?.Collections?.Select(c => c.Name ?? "").ToList() ?? [];
    }

    public async Task UpsertAsync(string collection, VectorPoint point, CancellationToken ct = default)
    {
        var request = new
        {
            points = new[]
            {
                new
                {
                    id = point.Id,
                    vector = point.Vector,
                    payload = point.Payload
                }
            }
        };
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/collections/{collection}/points", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpsertBatchAsync(string collection, List<VectorPoint> points, CancellationToken ct = default)
    {
        var request = new
        {
            points = points.Select(p => new
            {
                id = p.Id,
                vector = p.Vector,
                payload = p.Payload
            }).ToList()
        };
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/collections/{collection}/points", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string collection, string id, CancellationToken ct = default)
    {
        var request = new { points = new[] { id } };
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/collections/{collection}/points/delete", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<SearchResult>> SearchAsync(string collection, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null, CancellationToken ct = default)
    {
        var request = new
        {
            vector = queryVector,
            limit,
            with_payload = true
        };
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/collections/{collection}/points/search", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: ct);

        return result?.Result?.Select(r => new SearchResult
        {
            Id = r.Id ?? "",
            Score = r.Score,
            Payload = r.Payload
        }).ToList() ?? [];
    }

    public async Task<List<VectorPoint>> ScrollAsync(string collection, int limit = 100, string? offset = null, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/collections/{collection}/points/scroll?limit={limit}&with_payload=true";
        if (offset != null) url += $"&offset={offset}";

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ScrollResponse>(cancellationToken: ct);

        return result?.Result?.Points?.Select(p => new VectorPoint
        {
            Id = p.Id ?? "",
            Payload = p.Payload ?? new()
        }).ToList() ?? [];
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/collections", ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private class CollectionsResponse
    {
        [JsonPropertyName("collections")]
        public List<CollectionInfo>? Collections { get; set; }
    }

    private class CollectionInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class SearchResponse
    {
        [JsonPropertyName("result")]
        public List<SearchResultItem>? Result { get; set; }
    }

    private class SearchResultItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("score")]
        public float Score { get; set; }
        [JsonPropertyName("payload")]
        public Dictionary<string, object>? Payload { get; set; }
    }

    private class ScrollResponse
    {
        [JsonPropertyName("result")]
        public ScrollResult? Result { get; set; }
    }

    private class ScrollResult
    {
        [JsonPropertyName("points")]
        public List<ScrollPoint>? Points { get; set; }
    }

    private class ScrollPoint
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("payload")]
        public Dictionary<string, object>? Payload { get; set; }
    }
}
