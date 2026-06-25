using System.Collections.Concurrent;

namespace NexusCode.Roslyn;

public sealed class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, (TValue Value, long Timestamp)> _cache = new();
    private readonly int _maxSize;
    private readonly TimeSpan _expiration;

    public int Count => _cache.Count;

    public LRUCache(int maxSize = 1000, TimeSpan? expiration = null)
    {
        _maxSize = maxSize;
        _expiration = expiration ?? TimeSpan.FromMinutes(30);
    }

    public TValue? Get(TKey key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTimeOffset.UtcNow.Ticks - entry.Timestamp < _expiration.Ticks)
            {
                _cache[key] = (entry.Value, DateTimeOffset.UtcNow.Ticks);
                return entry.Value;
            }
            _cache.TryRemove(key, out _);
        }
        return default;
    }

    public void Set(TKey key, TValue value)
    {
        if (_cache.Count >= _maxSize)
            Evict();

        _cache[key] = (value, DateTimeOffset.UtcNow.Ticks);
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        var result = Get(key);
        value = result;
        return result != null;
    }

    public void Evict()
    {
        var now = DateTimeOffset.UtcNow.Ticks;
        var expired = _cache.Where(kvp => now - kvp.Value.Timestamp > _expiration.Ticks)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expired)
            _cache.TryRemove(key, out _);

        if (_cache.Count >= _maxSize)
        {
            var oldest = _cache.OrderBy(kvp => kvp.Value.Timestamp)
                .Take(_cache.Count / 4)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldest)
                _cache.TryRemove(key, out _);
        }
    }

    public void Clear() => _cache.Clear();
}
