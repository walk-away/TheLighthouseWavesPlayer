using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class UserSettings
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    public bool IsAutoplayEnabled { get; set; }

    public bool IsInfinitePlaybackEnabled { get; set; }

    public string AspectMode { get; set; }

    public double Brightness { get; set; }
}