namespace TheLighthouseWavesPlayer.Core.Models;

public class Artist
{
    public Guid ArtistId { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public string Genre { get; set; }
    
    public List<Album> Albums { get; set; } = new List<Album>();

    public Artist() { }

    public Artist(string name, string genre)
    {
        Name = name;
        Genre = genre;
    }
}