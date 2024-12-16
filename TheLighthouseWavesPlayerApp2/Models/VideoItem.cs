using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class VideoItem
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [MaxLength(255)] public string Title { get; set; }

    public string FilePath { get; set; }

    public bool IsFavorite { get; set; }

    public int Rating { get; set; }

    public DateTime LastPlayed { get; set; }
}