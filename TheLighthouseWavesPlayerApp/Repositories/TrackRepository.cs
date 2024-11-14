using SQLite;
using TheLighthouseWavesPlayer.Core.Models;
using TheLighthouseWavesPlayerApp.Data;
using TheLighthouseWavesPlayerApp.Repositories.Interfaces;

namespace TheLighthouseWavesPlayerApp.Repositories;

public class TrackRepository : ITrackRepository
{
    private readonly SQLiteAsyncConnection _database;

    public TrackRepository()
    {
        _database = DatabaseProvider.DatabaseAsync;
    }

    public async Task<Track> GetTrackAsync(Guid trackId) =>
        await _database.FindAsync<Track>(trackId);

    public async Task<IEnumerable<Track>> GetTracksByAlbumAsync(Guid albumId) =>
        await _database.Table<Track>().Where(t => t.AlbumId == albumId).ToListAsync();

    public async Task AddTrackAsync(Track track) =>
        await _database.InsertAsync(track);
    
    public async Task DeleteTrackAsync(Guid trackId)
    {
        var track = await GetTrackAsync(trackId);
        if (track != null)
        {
            await _database.DeleteAsync(track);
        }
    }
}