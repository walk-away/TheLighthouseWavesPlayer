namespace TheLighthouseWavesPlayer.Core.Models.Explorer;

public class FolderItem : ExplorerItem
{
    public string? ParentPath { get; set; }

    public FolderItem()
        : base(string.Empty)
    {
    }

    public FolderItem(string path, string parentPath)
        : base(path)
    {
        Name = new DirectoryInfo(path).Name;
        ParentPath = parentPath;
    }
}