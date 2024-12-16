using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class Bookmark
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int VideoItemId { get; set; }

    [Required]
    public TimeSpan Time { get; set; }

    [SQLite.MaxLength(255)]
    public string Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Ignore]
    public VideoItem Video { get; set; }
}