namespace TheLighthouseWavesPlayer.Core.Models;

public class PlaybackHistory
{
    public Guid PlaybackHistoryId { get; set; } = Guid.NewGuid();
    public Guid TrackId { get; set; }
    public DateTime DatePlayed { get; set; } = DateTime.Now;

    public Track Track { get; set; }

    public PlaybackHistory() { }

    public PlaybackHistory(Guid trackId)
    {
        TrackId = trackId;
    }
}