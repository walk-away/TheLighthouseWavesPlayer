using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class PlaylistVideo
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    public int PlaylistId { get; set; }

    public int VideoId { get; set; }
}