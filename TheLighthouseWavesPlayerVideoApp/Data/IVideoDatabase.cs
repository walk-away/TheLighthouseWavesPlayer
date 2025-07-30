using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Data;

public interface IVideoDatabase
{
    Task<List<VideoInfo>> GetFavoritesAsync();
    Task<VideoInfo?> GetFavoriteAsync(string filePath);
    Task<int> SaveFavoriteAsync(VideoInfo item);
    Task<int> UpdateVideoInfoAsync(VideoInfo item);
    Task<int> DeleteFavoriteAsync(VideoInfo item);
    Task<int> DeleteFavoriteByPathAsync(string filePath);
    
    Task<List<Playlist>> GetPlaylistsAsync();
    Task<Playlist?> GetPlaylistAsync(int playlistId);
    Task<int> SavePlaylistAsync(Playlist playlist);
    Task<int> DeletePlaylistAsync(Playlist playlist);
    Task<List<PlaylistItem>> GetPlaylistItemsAsync(int playlistId);
    Task<int> AddVideoToPlaylistAsync(int playlistId, string videoPath);
    Task<int> RemoveVideoFromPlaylistAsync(int playlistId, string videoPath);
    Task<int> UpdatePlaylistItemOrderAsync(List<PlaylistItem> items);
    Task<int> ReorderPlaylistItemAsync(int itemId, int newOrder);
    Task<VideoInfo?> GetOrCreateVideoInfoAsync(string filePath, VideoInfo? videoInfo = null);
}