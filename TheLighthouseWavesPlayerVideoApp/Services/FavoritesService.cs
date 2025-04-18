using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class FavoritesService : IFavoritesService
{
    private readonly VideoDatabase _database;

    public FavoritesService(VideoDatabase database)
    {
        _database = database;
    }

    public async Task AddFavoriteAsync(VideoInfo video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath)) return;
        await _database.SaveFavoriteAsync(video);
    }

    public async Task<IList<VideoInfo>> GetFavoritesAsync()
    {
        return await _database.GetFavoritesAsync();
    }

    public async Task<bool> IsFavoriteAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        var favorite = await _database.GetFavoriteAsync(filePath);
        return favorite != null;
    }

    public async Task RemoveFavoriteAsync(VideoInfo video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath)) return;
        await _database.DeleteFavoriteByPathAsync(video.FilePath);
    }
}