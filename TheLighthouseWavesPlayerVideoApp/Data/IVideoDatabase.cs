using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Data;

public interface IVideoDatabase
{
    Task<List<VideoInfo>> GetFavoritesAsync();
    Task<VideoInfo> GetFavoriteAsync(string filePath);
    Task<int> SaveFavoriteAsync(VideoInfo item);
    Task<int> DeleteFavoriteAsync(VideoInfo item);
    Task<int> DeleteFavoriteByPathAsync(string filePath);
}