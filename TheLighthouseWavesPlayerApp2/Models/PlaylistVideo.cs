using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class PlaylistVideo
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int PlaylistId { get; set; }

    [Indexed]
    public int VideoId { get; set; }

    public int Order { get; set; }

    [Ignore]
    public Playlist Playlist { get; set; }

    [Ignore]
    public VideoItem Video { get; set; }
}