using TheLighthouseWavesPlayer.Core.Models.Explorer;

namespace TheLighthouseWavesPlayer.Core.Interfaces.Explorer;

public interface IExplorerService
{
    List<ExplorerItem> LoadDirectoryItems(string folderPath);
}