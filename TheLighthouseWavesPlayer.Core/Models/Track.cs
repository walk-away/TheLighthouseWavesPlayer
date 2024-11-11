namespace TheLighthouseWavesPlayer.Core.Models;

public class Track
{
    public Guid TrackId { get; set; } = Guid.NewGuid();
    public string Title { get; set; }
    public TimeSpan Duration { get; set; }
    public string DurationText => $"{Duration.Minutes}:{Duration.Seconds:D2}";
    public Guid AlbumId { get; set; }
    public Album Album { get; set; }

    public Track() { }

    public Track(string title, TimeSpan duration, Guid albumId)
    {
        Title = title;
        Duration = duration;
        AlbumId = albumId;
    }
}