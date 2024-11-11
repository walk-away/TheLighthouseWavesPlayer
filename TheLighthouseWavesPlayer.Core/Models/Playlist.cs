namespace TheLighthouseWavesPlayer.Core.Models;

public class Playlist
{
    public Guid PlaylistId { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public List<Track> Tracks { get; set; } = new List<Track>();
    public List<Album> Albums { get; set; } = new List<Album>();
    public bool IsHidden { get; set; }
    public bool IsRemovable { get; set; }
    public ImageSource PlaylistArt { get; set; }

    public Playlist() { }

    public Playlist(string name)
    {
        Name = name;
    }
}