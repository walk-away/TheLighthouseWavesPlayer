using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.Repositories.Interfaces;

public interface IPlaybackHistoryRepository
{
    Task<IEnumerable<PlaybackHistory>> GetPlaybackHistoryAsync();
    Task AddPlaybackHistoryAsync(PlaybackHistory playbackHistory);
}