using SQLite;

namespace TheLighthouseWavesPlayerVideoApp.Models;

[Table("playlist_items")]
public class PlaylistItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int PlaylistId { get; set; }

    public string VideoPath { get; set; } = string.Empty;

    public int Order { get; set; }

    public DateTime AddedDate { get; set; } = DateTime.Now;

    [Ignore]
    public VideoInfo? VideoInfo { get; set; }
}