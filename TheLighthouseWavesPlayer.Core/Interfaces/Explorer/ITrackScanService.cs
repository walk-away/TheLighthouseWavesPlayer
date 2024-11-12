using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayer.Core.Interfaces.Explorer;

public interface ITrackScanService
{
    List<Track> SearchTracks(List<ScanAudioFolder> folders);
}