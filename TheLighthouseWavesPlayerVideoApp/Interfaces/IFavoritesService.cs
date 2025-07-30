using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Interfaces;

public interface IFavoritesService
{
    Task<IList<VideoInfo>> GetFavoritesAsync();
    Task AddFavoriteAsync(VideoInfo? video);
    Task RemoveFavoriteAsync(VideoInfo? video);
    Task<bool> IsFavoriteAsync(string filePath);
}