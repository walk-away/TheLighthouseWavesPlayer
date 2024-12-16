using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class Bookmark
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int VideoItemId { get; set; }
    public TimeSpan Time { get; set; }
    [MaxLength(255)] public string Note { get; set; }
}