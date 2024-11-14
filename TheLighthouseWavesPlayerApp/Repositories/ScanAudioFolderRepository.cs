using SQLite;
using TheLighthouseWavesPlayer.Core.Models;
using TheLighthouseWavesPlayerApp.Data;
using TheLighthouseWavesPlayerApp.Repositories.Interfaces;

namespace TheLighthouseWavesPlayerApp.Repositories;

public class ScanAudioFolderRepository : IScanAudioFolderRepository
{
    private readonly SQLiteAsyncConnection _database;

    public ScanAudioFolderRepository()
    {
        _database = DatabaseProvider.DatabaseAsync;
    }

    public async Task AddScanFolderAsync(string path)
    {
        var folder = new ScanAudioFolder(path);
        await _database.InsertAsync(folder);
    }

    public async Task RemoveScanFolderAsync(string path)
    {
        var folder = await _database.Table<ScanAudioFolder>()
            .Where(f => f.Path == path)
            .FirstOrDefaultAsync();
        if (folder != null)
        {
            await _database.DeleteAsync(folder);
        }
    }

    public async Task<List<ScanAudioFolder>> GetAllScanFoldersAsync()
    {
        return await _database.Table<ScanAudioFolder>().ToListAsync();
    }
}