using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.AI;

public interface IAiService
{
    Task<string?> GetInfoByTrack(Track track);
    Task<string?> GetInfoByAlbum(Album album);
    Task<string?> GetInfoByArtist(Artist artist);
}