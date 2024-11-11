namespace TheLighthouseWavesPlayer.Core.Models;

public class Favorite
{
    public Guid FavoriteId { get; set; } = Guid.NewGuid();
    public Guid? AlbumId { get; set; }
    public Guid? TrackId { get; set; }
    public Guid? ArtistId { get; set; }

    public Album Album { get; set; }
    public Track Track { get; set; }
    public Artist Artist { get; set; }

    public Favorite() { }
}