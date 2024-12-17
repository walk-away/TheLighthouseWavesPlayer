using TheLighthouseWavesPlayerApp2.Models;

namespace TheLighthouseWavesPlayerApp2.Services.Interfaces;

public interface IPlaylistService
{
    Task<int> AddPlaylistAsync(Playlist playlist);
    Task<List<Playlist>> GetPlaylistsAsync();
    Task<Playlist> GetPlaylistByIdAsync(int id);
    Task<int> UpdatePlaylistAsync(Playlist playlist);
    Task<int> DeletePlaylistAsync(int id);
    Task<int> AddVideoToPlaylistAsync(int playlistId, int videoId);
    Task<List<VideoItem>> GetVideosInPlaylistAsync(int playlistId);
    Task<int> RemoveVideoFromPlaylistAsync(int playlistId, int videoId);
}