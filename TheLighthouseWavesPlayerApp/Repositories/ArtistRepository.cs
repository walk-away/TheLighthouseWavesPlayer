using SQLite;
using TheLighthouseWavesPlayer.Core.Models;
using TheLighthouseWavesPlayerApp.Data;
using TheLighthouseWavesPlayerApp.Repositories.Interfaces;

namespace TheLighthouseWavesPlayerApp.Repositories;

public class ArtistRepository : IArtistRepository
{
    private readonly SQLiteAsyncConnection _database;

    public ArtistRepository()
    {
        _database = DatabaseProvider.DatabaseAsync;
    }

    public async Task<Artist> GetArtistAsync(Guid artistId) =>
        await _database.FindAsync<Artist>(artistId);

    public async Task<IEnumerable<Artist>> GetAllArtistsAsync() =>
        await _database.Table<Artist>().ToListAsync();

    public async Task AddArtistAsync(Artist artist) =>
        await _database.InsertAsync(artist);
    
    public async Task DeleteArtistAsync(Guid artistId)
    {
        var artist = await GetArtistAsync(artistId);
        if (artist != null)
        {
            await _database.DeleteAsync(artist);
        }
    }
}