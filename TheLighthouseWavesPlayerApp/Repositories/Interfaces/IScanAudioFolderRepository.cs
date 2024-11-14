using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.Repositories.Interfaces;

public interface IScanAudioFolderRepository
{
    Task AddScanFolderAsync(string path);
    Task RemoveScanFolderAsync(string path);
    Task<List<ScanAudioFolder>> GetAllScanFoldersAsync();
}