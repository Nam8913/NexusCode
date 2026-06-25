using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace NexusCode.Embedding;

public sealed class EmbeddingEngine : IDisposable
{
    private readonly OllamaClient _ollama;
    private readonly ConcurrentDictionary<string, float[]> _cache = new();
    private readonly string _model;
    private readonly int _dimension;

    public EmbeddingEngine(string model = "nomic-embed-text", string ollamaUrl = "http://localhost:11434")
    {
        _model = model;
        _ollama = new OllamaClient(ollamaUrl);
        _dimension = model switch
        {
            "nomic-embed-text" => 768,
            "mxbai-embed-large" => 1024,
            "bge-m3" => 1024,
            _ => 768
        };
    }

    public int Dimension => _dimension;
    public string Model => _model;

    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var hash = ComputeHash(text);
        if (_cache.TryGetValue(hash, out var cached))
            return cached;

        var vector = await _ollama.EmbedAsync(text, _model, ct);
        if (vector != null)
        {
            _cache[hash] = vector;
        }
        return vector;
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(string[] texts, CancellationToken ct = default)
    {
        var results = new List<float[]>();
        var toEmbed = new List<int>();

        for (int i = 0; i < texts.Length; i++)
        {
            var hash = ComputeHash(texts[i]);
            if (_cache.TryGetValue(hash, out var cached))
            {
                results.Add(cached);
            }
            else
            {
                results.Add(new float[_dimension]);
                toEmbed.Add(i);
            }
        }

        if (toEmbed.Count > 0)
        {
            var textsToEmbed = toEmbed.Select(i => texts[i]).ToArray();
            var embedded = await _ollama.EmbedBatchAsync(textsToEmbed, _model, ct);

            if (embedded != null)
            {
                for (int j = 0; j < toEmbed.Count && j < embedded.Length; j++)
                {
                    var idx = toEmbed[j];
                    results[idx] = embedded[j];
                    _cache[ComputeHash(texts[idx])] = embedded[j];
                }
            }
        }

        return results;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return await _ollama.IsAvailableAsync(ct);
    }

    private static string ComputeHash(string text)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        _cache.Clear();
    }
}
