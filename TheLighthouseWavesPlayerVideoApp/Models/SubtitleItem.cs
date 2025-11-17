namespace TheLighthouseWavesPlayerVideoApp.Models;

public class SubtitleItem
{
    public int Index { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Text { get; set; } = string.Empty;

    public bool IsActiveAt(TimeSpan position)
    {
        return position >= StartTime && position <= EndTime;
    }
}
