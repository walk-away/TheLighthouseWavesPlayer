namespace TheLighthouseWavesPlayerApp2.Services.Interfaces;

public interface IMemoryCacheService
{
    bool TryGetValue<T>(string key, out T value);
    void Set<T>(string key, T value, TimeSpan duration);
    void Remove(string key);
}