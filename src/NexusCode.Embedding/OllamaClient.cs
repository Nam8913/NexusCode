using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusCode.Embedding;

public sealed class OllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OllamaClient(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task<float[]?> EmbedAsync(string input, string model = "nomic-embed-text", CancellationToken ct = default)
    {
        try
        {
            var request = new { model, input = new[] { input } };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/embed", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct);
            return result?.Embeddings?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task<float[][]?> EmbedBatchAsync(string[] inputs, string model = "nomic-embed-text", CancellationToken ct = default)
    {
        try
        {
            var request = new { model, input = inputs };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/embed", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct);
            return result?.Embeddings;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ModelsResponse>(cancellationToken: ct);
            return result?.Models?.Select(m => m.Name ?? "").ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private class EmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public float[][]? Embeddings { get; set; }
    }

    private class ModelsResponse
    {
        [JsonPropertyName("models")]
        public List<ModelInfo>? Models { get; set; }
    }

    private class ModelInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
