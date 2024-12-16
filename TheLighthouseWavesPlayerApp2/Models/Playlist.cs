using System.ComponentModel.DataAnnotations;
using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class Playlist
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Required, SQLite.MaxLength(255)]
    public string Name { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    [Ignore]
    public List<VideoItem> Videos { get; set; }
}