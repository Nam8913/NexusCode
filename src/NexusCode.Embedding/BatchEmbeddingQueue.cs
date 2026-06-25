using System.Collections.Concurrent;

namespace NexusCode.Embedding;

public sealed class BatchEmbeddingQueue : IDisposable
{
    private readonly EmbeddingEngine _engine;
    private readonly ConcurrentQueue<EmbeddingJob> _queue = new();
    private readonly ConcurrentDictionary<string, float[]> _cache = new();
    private readonly int _batchSize;
    private readonly int _maxConcurrent;
    private CancellationTokenSource? _cts;
    private Task? _processingTask;

    public event Action<int, int>? ProgressChanged;

    public int PendingCount => _queue.Count;
    public int CacheCount => _cache.Count;

    public BatchEmbeddingQueue(EmbeddingEngine engine, int batchSize = 32, int maxConcurrent = 4)
    {
        _engine = engine;
        _batchSize = batchSize;
        _maxConcurrent = maxConcurrent;
    }

    public string Enqueue(string id, string text)
    {
        var hash = ComputeHash(text);
        if (_cache.ContainsKey(hash))
            return hash;

        _queue.Enqueue(new EmbeddingJob { Id = id, Text = text, Hash = hash });
        return hash;
    }

    public void StartProcessing()
    {
        _cts = new CancellationTokenSource();
        _processingTask = ProcessQueueAsync(_cts.Token);
    }

    public void StopProcessing()
    {
        _cts?.Cancel();
        _processingTask?.Wait(TimeSpan.FromSeconds(5));
    }

    public float[]? GetCached(string hash)
    {
        return _cache.TryGetValue(hash, out var vector) ? vector : null;
    }

    public bool HasCached(string hash)
    {
        return _cache.ContainsKey(hash);
    }

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var batch = new List<EmbeddingJob>();

            while (batch.Count < _batchSize && _queue.TryDequeue(out var job))
            {
                if (!_cache.ContainsKey(job.Hash))
                    batch.Add(job);
            }

            if (batch.Count > 0)
            {
                try
                {
                    var texts = batch.Select(j => j.Text).ToArray();
                    var vectors = await _engine.GetEmbeddingsAsync(texts, ct);

                    for (int i = 0; i < batch.Count && i < vectors.Count; i++)
                    {
                        _cache[batch[i].Hash] = vectors[i];
                    }

                    ProgressChanged?.Invoke(_cache.Count, _queue.Count);
                }
                catch
                {
                    foreach (var job in batch)
                        _queue.Enqueue(job);
                }
            }

            await Task.Delay(100, ct).ConfigureAwait(false);
        }
    }

    private static string ComputeHash(string text)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        StopProcessing();
        _cts?.Dispose();
        _cache.Clear();
    }

    private class EmbeddingJob
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }
}
