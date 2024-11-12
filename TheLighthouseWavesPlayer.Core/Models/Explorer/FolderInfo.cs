namespace TheLighthouseWavesPlayer.Core.Models.Explorer;

public class FolderInfo
{
    public string Path { get; private set; }
    public string Name { get; private set; }
    public int? Number { get; private set; }
    public bool IsNumeric => Number.HasValue;
    public bool HasChildren => Children?.Count > 0;
    public bool HasAudio => AudioFiles?.Count > 0;
    public FolderInfo? Parent { get; private set; }
    public List<FolderInfo> Children { get; private set; } = new List<FolderInfo>();
    public List<string> AudioFiles { get; private set; } = new List<string>();

    public FolderInfo(string path)
    {
        Path = path;
        Name = new DirectoryInfo(path).Name;
        if (int.TryParse(Name, out int val))
        {
            Number = val;
        }

        SearchSubFolders(path, this);
    }

    public FolderInfo(string path, FolderInfo parent)
        : this(path)
    {
        Parent = parent;
    }

    public static List<FolderInfo> GetLeaves(FolderInfo folder)
    {
        var result = new List<FolderInfo>();
        if (folder.Children.Count == 0 && folder.AudioFiles.Count > 0)
        {
            result.Add(folder);
        }

        foreach (var child in folder.Children.OrderBy(a => a.Name))
        {
            result.AddRange(GetLeaves(child));
        }

        return result;
    }

    public override string ToString()
    {
        return Name;
    }

    private void SearchSubFolders(string path, FolderInfo parent)
    {
        Constants.AudioFiles
            .ToList()
            .ForEach(e => AudioFiles.AddRange(Directory.GetFiles(path, $"*{e}", SearchOption.TopDirectoryOnly)));


        foreach (var dir in Directory.GetDirectories(path))
        {
            var childFolder = new FolderInfo(dir, parent);
            if (childFolder.HasChildren || childFolder.HasAudio)
            {
                parent.Children.Add(childFolder);
            }
        }
    }
}