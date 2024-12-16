using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class Playlist
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [MaxLength(255)] public string Name { get; set; }

    public DateTime CreatedDate { get; set; }
}