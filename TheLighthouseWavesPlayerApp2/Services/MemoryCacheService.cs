using System.Collections.Concurrent;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.Services;

public class MemoryCacheService : IMemoryCacheService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    private class CacheItem
    {
        public object Value { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        value = default;
        if (_cache.TryGetValue(key, out var item) && item.ExpiresAt > DateTime.UtcNow)
        {
            value = (T)item.Value;
            return true;
        }

        return false;
    }

    public void Set<T>(string key, T value, TimeSpan duration)
    {
        var item = new CacheItem
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(duration)
        };
        _cache.AddOrUpdate(key, item, (_, _) => item);
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }
}