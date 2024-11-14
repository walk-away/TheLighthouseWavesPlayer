using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.Repositories.Interfaces;

public interface IArtistRepository
{
    Task<Artist> GetArtistAsync(Guid artistId);
    Task<IEnumerable<Artist>> GetAllArtistsAsync();
    Task AddArtistAsync(Artist artist);
    Task DeleteArtistAsync(Guid artistId);
}