using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace TheLighthouseWavesPlayerVideoApp.Models;

[Table("favorites")] // Define table name for SQLite
public partial class VideoInfo : ObservableObject
{
    [PrimaryKey, AutoIncrement] 
    public int Id { get; set; }
    
    public string Title { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string? ThumbnailPath { get; set; }
    public long DurationMilliseconds { get; set; } // Store duration if available from MediaStore

    [Ignore] // Don't store IsFavorite directly in this table if using a separate favorites mechanism
    public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMilliseconds);

    // IsFavorite will be managed by the FavoritesService, not stored directly here
    // We'll add a property in ViewModels or use the service to check status
}