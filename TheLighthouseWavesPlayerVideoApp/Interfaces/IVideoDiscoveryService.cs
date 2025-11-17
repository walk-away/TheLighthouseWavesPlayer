using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Interfaces;

public interface IVideoDiscoveryService
{
    Task<IList<VideoInfo>> DiscoverVideosAsync();
    Task<bool> RequestPermissionsAsync();
}
