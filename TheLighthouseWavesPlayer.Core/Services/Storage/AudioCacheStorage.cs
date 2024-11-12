using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TheLighthouseWavesPlayer.Core.Interfaces.Storage;
using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayer.Core.Services.Storage;

public class AudioCacheStorage : IAudioCacheStorage
{
    private readonly string _cacheDirectory;
    private readonly ILogger<AudioCacheStorage> _logger;

    public AudioCacheStorage(string cacheDirectory, ILogger<AudioCacheStorage> logger)
    {
        _cacheDirectory = cacheDirectory;
        _logger = logger;
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<string> GetOrAddAsync(Playlist playlist, Func<Playlist, Task<AudioCacheMetadata>> delegateFunc)
    {
        var fileName = Preferences.Get($"music-{playlist.PlaylistId}", "");
        if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
        {
            return fileName;
        }

        var metadata = await delegateFunc(playlist);
        if (metadata == null)
        {
            return default;
        }

        var cacheFileNameOnly = $"{playlist.PlaylistId}{metadata.FileExtension}";
        var cachePath = Path.Combine(_cacheDirectory, cacheFileNameOnly);
        await File.WriteAllBytesAsync(cachePath, metadata.Buffer);

        Preferences.Set($"music-{playlist.PlaylistId}", cachePath);
        return cachePath;
    }

    public Task CalcCacheSizeAsync(Action<double> delegage)
    {
        var files = Directory.GetFiles(_cacheDirectory);
        foreach (var file in files)
        {
            var fi = new FileInfo(file);
            delegage(fi.Length);
        }
        return Task.CompletedTask;
    }

    public Task ClearCacheAsync()
    {
        var files = Directory.GetFiles(_cacheDirectory);
        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file.");
            }
        }
        return Task.CompletedTask;
    }
}