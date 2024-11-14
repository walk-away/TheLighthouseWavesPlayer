using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.Repositories.Interfaces;

public interface ITrackRepository
{
    Task<Track> GetTrackAsync(Guid trackId);
    Task<IEnumerable<Track>> GetTracksByAlbumAsync(Guid albumId);
    Task AddTrackAsync(Track track);
    Task DeleteTrackAsync(Guid trackId);
}