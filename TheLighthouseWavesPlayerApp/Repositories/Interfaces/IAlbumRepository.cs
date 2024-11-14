using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.Repositories.Interfaces;

public interface IAlbumRepository
{
    Task<Album> GetAlbumAsync(Guid albumId);
    Task<IEnumerable<Album>> GetAllAlbumsAsync();
    Task AddAlbumAsync(Album album);
    Task DeleteAlbumAsync(Guid albumId);
}