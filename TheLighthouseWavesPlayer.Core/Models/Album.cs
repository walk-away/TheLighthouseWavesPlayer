namespace TheLighthouseWavesPlayer.Core.Models;

public class Album
{
    public Guid AlbumId { get; set; } = Guid.NewGuid();
    public string Title { get; set; }
    public string Genre { get; set; }
    public int ReleaseYear { get; set; }
    public ImageSource AlbumArt { get; set; }
    public string AlbumArtPath { get; set; }
    public List<Track> Tracks { get; set; } = new List<Track>();

    public Album()
    {
    }

    public Album(string title, string genre, int releaseYear)
    {
        Title = title;
        Genre = genre;
        ReleaseYear = releaseYear;
    }
}