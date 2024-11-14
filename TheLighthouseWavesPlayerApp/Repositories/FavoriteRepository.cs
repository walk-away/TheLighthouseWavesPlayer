using SQLite;
using TheLighthouseWavesPlayer.Core.Models;
using TheLighthouseWavesPlayerApp.Data;
using TheLighthouseWavesPlayerApp.Repositories.Interfaces;

namespace TheLighthouseWavesPlayerApp.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly SQLiteAsyncConnection _database;

    public FavoriteRepository()
    {
        _database = DatabaseProvider.DatabaseAsync;
    }

    public async Task<IEnumerable<Favorite>> GetAllFavoritesAsync() =>
        await _database.Table<Favorite>().ToListAsync();

    public async Task AddToFavoritesAsync(Favorite favorite) =>
        await _database.InsertAsync(favorite);

    public async Task RemoveFromFavoritesAsync(Guid favoriteId)
    {
        var favorite = await _database.FindAsync<Favorite>(favoriteId);
        if (favorite != null)
        {
            await _database.DeleteAsync(favorite);
        }
    }
}