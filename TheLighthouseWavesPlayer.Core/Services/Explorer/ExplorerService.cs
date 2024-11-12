using TheLighthouseWavesPlayer.Core.Interfaces.Explorer;
using TheLighthouseWavesPlayer.Core.Models.Explorer;

namespace TheLighthouseWavesPlayer.Core.Services.Explorer;

public abstract class ExplorerService : IExplorerService
{
    public abstract List<ExplorerItem> LoadDirectoryItems(string folderPath);

    protected List<ExplorerItem> GetChildren(string path)
    {
        var result = new List<ExplorerItem>();
        foreach (var dir in Directory.GetDirectories(path))
        {
            result.Add(new FolderItem(dir, path));
        }

        foreach (var file in Directory.GetFiles(path))
        {
            result.Add(new FileItem(file));
        }

        return result;
    }
}