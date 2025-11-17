using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace TheLighthouseWavesPlayerVideoApp.Models;

[Table("favorites")]
public partial class VideoInfo : ObservableObject
{
    public string Title { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string? ThumbnailPath { get; set; }
    public long DurationMilliseconds { get; set; }

    [ObservableProperty]
    private bool _isFavorite;

    [Ignore]
    private TimeSpan Duration => DurationMilliseconds > 0
        ? TimeSpan.FromMilliseconds(DurationMilliseconds)
        : TimeSpan.Zero;

    [Ignore]
    public string FormattedDuration => DurationMilliseconds > 0
        ? Duration.ToString(@"hh\:mm\:ss")
        : "Unknown";
}
