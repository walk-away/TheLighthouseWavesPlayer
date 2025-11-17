using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace TheLighthouseWavesPlayerVideoApp.Models;

[Table("playlists")]
public partial class Playlist : ObservableObject
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [ObservableProperty] private string? _name = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime LastModified { get; set; } = DateTime.Now;

    [ObservableProperty] private string _thumbnailPath = string.Empty;

    [Ignore] public int VideoCount { get; set; }

    [Ignore] public TimeSpan TotalDuration { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoCountDisplay))]
    private int _displayVideoCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalDurationDisplay))]
    private TimeSpan _displayTotalDuration;

    [ObservableProperty]
    private bool _isSelected;

    public string VideoCountDisplay => $"{DisplayVideoCount} videos";
    public string TotalDurationDisplay => DisplayTotalDuration.ToString(@"hh\:mm\:ss");
}
