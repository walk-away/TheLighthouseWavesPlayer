namespace TheLighthouseWavesPlayer.Core.Models;

public class TrackMetadata
{
    public Guid TrackMetadataId { get; set; } = Guid.NewGuid();
    public Guid TrackId { get; set; }
    public string Lyrics { get; set; }
    public string Composer { get; set; }

    public Track Track { get; set; }

    public TrackMetadata() { }

    public TrackMetadata(Guid trackId, string lyrics, string composer)
    {
        TrackId = trackId;
        Lyrics = lyrics;
        Composer = composer;
    }
}