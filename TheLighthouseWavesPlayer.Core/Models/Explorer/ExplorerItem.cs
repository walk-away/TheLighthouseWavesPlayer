namespace TheLighthouseWavesPlayer.Core.Models.Explorer;

public abstract class ExplorerItem
{
    public string Path { get; set; }
    public string Name { get; set; }

    public ExplorerItem(string path)
    {
        Path = path;
        Name = string.Empty;
    }
}