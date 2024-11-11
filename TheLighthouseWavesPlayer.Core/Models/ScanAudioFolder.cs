namespace TheLighthouseWavesPlayer.Core.Models;

public class ScanAudioFolder
{
    public ScanAudioFolder()
    {
        Path = string.Empty;
    }

    public ScanAudioFolder(string path)
        : this()
    {
        Path = path;
    }

    public string Path { get; set; }
}