using SQLite;

namespace TheLighthouseWavesPlayerVideoApp.Models;

public class Video
{
    [PrimaryKey, AutoIncrement] 
    public int Id { get; set; }
        
    public string Title { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string? ThumbnailPath { get; set; }
    public DateTime DateAdded { get; set; }
    public bool IsFavorite { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan LastPosition { get; set; }
    
    public DateTime LastPlayed { get; set; }
}