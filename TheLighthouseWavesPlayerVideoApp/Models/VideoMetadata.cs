namespace TheLighthouseWavesPlayerVideoApp.Models;

public class VideoMetadata
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FormattedFileSize => GetFormattedFileSize();
    public DateTime LastModified { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }

    private string GetFormattedFileSize()
    {
        if (FileSize < 1024)
        {
            return $"{FileSize} B";
        }
        else if (FileSize < 1024 * 1024)
        {
            return $"{FileSize / 1024.0:F2} KB";
        }
        else if (FileSize < 1024 * 1024 * 1024)
        {
            return $"{FileSize / (1024.0 * 1024):F2} MB";
        }
        else
        {
            return $"{FileSize / (1024.0 * 1024 * 1024):F2} GB";
        }
    }
}
