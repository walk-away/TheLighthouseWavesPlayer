using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.Repositories.Interfaces;

public interface IFavoriteRepository
{
    Task<IEnumerable<Favorite>> GetAllFavoritesAsync();
    Task AddToFavoritesAsync(Favorite favorite);
    Task RemoveFromFavoritesAsync(Guid favoriteId);
}