namespace TheLighthouseWavesPlayer.Core.Models.Explorer;

public class FileItem : ExplorerItem
{
    public bool Audio { get; private set; }

    public FileItem(string path)
        : base(path)
    {
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        var extension = System.IO.Path.GetExtension(path);
        Audio = Constants.AudioFiles.Contains(extension.ToLower());
    }
}