namespace TheLighthouseWavesPlayer.Core.Models;

public class AudioCacheMetadata
{
    public string FileExtension { get; set; }
    public byte[] Buffer { get; set; }
    public AudioCacheMetadata(string fileExtension, byte[] buffer)
    {
        FileExtension = fileExtension;
        Buffer = buffer;
    }
}