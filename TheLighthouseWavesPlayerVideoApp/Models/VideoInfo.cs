using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace TheLighthouseWavesPlayerVideoApp.Models;

[Table("favorites")]
public partial class VideoInfo : ObservableObject
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    public string Title { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string? ThumbnailPath { get; set; }
    public long DurationMilliseconds { get; set; }
    
    [ObservableProperty]
    private bool _isFavorite;

    [Ignore] public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMilliseconds);
    
    [Ignore] public string FormattedDuration => Duration.ToString(@"hh\:mm\:ss");
}