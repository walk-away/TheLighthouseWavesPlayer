using SQLite;
using TheLighthouseWavesPlayer.Core.Models;
using TheLighthouseWavesPlayerApp.Data;
using TheLighthouseWavesPlayerApp.Repositories.Interfaces;

namespace TheLighthouseWavesPlayerApp.Repositories;

public class AlbumRepository : IAlbumRepository
{
    private readonly SQLiteAsyncConnection _database;

    public AlbumRepository()
    {
        _database = DatabaseProvider.DatabaseAsync;
    }

    public async Task<Album> GetAlbumAsync(Guid albumId) =>
        await _database.FindAsync<Album>(albumId);

    public async Task<IEnumerable<Album>> GetAllAlbumsAsync() =>
        await _database.Table<Album>().ToListAsync();

    public async Task AddAlbumAsync(Album album) =>
        await _database.InsertAsync(album);
    
    public async Task DeleteAlbumAsync(Guid albumId)
    {
        var album = await GetAlbumAsync(albumId);
        if (album != null)
        {
            await _database.DeleteAsync(album);
        }
    }
}