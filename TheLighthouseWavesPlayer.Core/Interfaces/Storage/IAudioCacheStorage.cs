using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayer.Core.Interfaces.Storage;

public interface IAudioCacheStorage
{
    Task<string> GetOrAddAsync(Playlist playlist, Func<Playlist, Task<AudioCacheMetadata>> delegateFunc);
    Task CalcCacheSizeAsync(Action<double> delegage);
    Task ClearCacheAsync();
}