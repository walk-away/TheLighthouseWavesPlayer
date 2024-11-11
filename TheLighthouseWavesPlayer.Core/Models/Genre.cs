namespace TheLighthouseWavesPlayer.Core.Models;

public class Genre
{
    public Guid GenreId { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public List<Album> Albums { get; set; } = new List<Album>();

    public Genre() { }

    public Genre(string name)
    {
        Name = name;
    }
}