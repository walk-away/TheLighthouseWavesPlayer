using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using SQLite;

namespace TheLighthouseWavesPlayerApp2.Models;

public class VideoItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    
    private string _title;
    private string _filePath;
    
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Required, SQLite.MaxLength(255)]
    public string Title 
    { 
        get => _title;
        set
        {
            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    [Required]
    public string FilePath 
    { 
        get => _filePath;
        set
        {
            _filePath = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
        }
    }

    public bool IsFavorite { get; set; }

    [Range(0, 5)]
    public int Rating { get; set; }

    public DateTime LastPlayed { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    [Ignore]
    public List<Bookmark> Bookmarks { get; set; }
}