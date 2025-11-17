using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Interfaces;

public interface IPlaylistService
{
    Task<List<Playlist>> GetPlaylistsAsync();
    Task<Playlist?> GetPlaylistAsync(int playlistId);
    Task<int> CreatePlaylistAsync(string name, string description = "");
    Task<int> UpdatePlaylistAsync(Playlist playlist);
    Task<int> DeletePlaylistAsync(Playlist playlist);

    Task<List<VideoInfo>> GetPlaylistVideosAsync(int playlistId);
    Task<int> AddVideoToPlaylistAsync(int playlistId, VideoInfo video);
    Task<int> RemoveVideoFromPlaylistAsync(int playlistId, string videoPath);
    Task<int> ReorderPlaylistAsync(int playlistId, List<PlaylistItem> items);
    Task<bool> IsVideoInPlaylistAsync(int playlistId, string videoFilePath);
    Task UpdatePlaylistStatisticsAsync(Playlist playlist);
}
