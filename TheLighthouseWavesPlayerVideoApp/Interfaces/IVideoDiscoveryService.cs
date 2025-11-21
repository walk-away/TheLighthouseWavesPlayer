using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Interfaces;

public interface IVideoDiscoveryService
{
    Task<PermissionStatus> RequestPermissionsAsync();
    Task<IList<VideoInfo>> DiscoverVideosAsync();
}
