using TheLighthouseWavesPlayerApp2.Database;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.Services;

public class PlaylistService : IPlaylistService
{
    private readonly DatabaseService _database;

    public PlaylistService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<int> AddPlaylistAsync(Playlist playlist)
    {
        return await _database.AddPlaylistAsync(playlist);
    }

    public async Task<List<Playlist>> GetPlaylistsAsync()
    {
        return await _database.GetPlaylistsAsync();
    }

    public async Task<Playlist> GetPlaylistByIdAsync(int id)
    {
        return await _database.GetPlaylistByIdAsync(id);
    }

    public async Task<int> UpdatePlaylistAsync(Playlist playlist)
    {
        return await _database.UpdatePlaylistAsync(playlist);
    }

    public async Task<int> DeletePlaylistAsync(int id)
    {
        return await _database.DeletePlaylistAsync(id);
    }

    public async Task<int> AddVideoToPlaylistAsync(int playlistId, int videoId)
    {
        return await _database.AddVideoToPlaylistAsync(playlistId, videoId);
    }

    public async Task<List<VideoItem>> GetVideosInPlaylistAsync(int playlistId)
    {
        return await _database.GetVideosInPlaylistAsync(playlistId);
    }

    public async Task<int> RemoveVideoFromPlaylistAsync(int playlistId, int videoId)
    {
        return await _database.RemoveVideoFromPlaylistAsync(playlistId, videoId);
    }
}