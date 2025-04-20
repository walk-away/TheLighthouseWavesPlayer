using SQLite;
using TheLighthouseWavesPlayerVideoApp.Constants;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Data;

public class VideoDatabase : IVideoDatabase
{
    SQLiteAsyncConnection _database;

    public VideoDatabase()
    {
    }

    async Task Init()
    {
        if (_database is not null)
            return;

        _database = new SQLiteAsyncConnection(DatabaseConstants.DatabasePath, DatabaseConstants.Flags);

        await _database.CreateTableAsync<VideoInfo>();
    }

    public async Task<List<VideoInfo>> GetFavoritesAsync()
    {
        await Init();

        return await _database.Table<VideoInfo>().ToListAsync();
    }

    public async Task<VideoInfo> GetFavoriteAsync(string filePath)
    {
        await Init();
        return await _database.Table<VideoInfo>().Where(i => i.FilePath == filePath).FirstOrDefaultAsync();
    }

    public async Task<int> SaveFavoriteAsync(VideoInfo item)
    {
        await Init();

        var existing = await GetFavoriteAsync(item.FilePath);
        if (existing == null)
        {
            // Insert new favorite
            return await _database.InsertAsync(item);
        }
        else
        {
            // Already exists, do nothing or update if needed (though VideoInfo details might change)
            // For simplicity, we just return 0 indicating no insert happened.
            // You could update if Title/Duration might change:
            // return await _database.UpdateAsync(item);
            return 0;
        }
    }

    public async Task<int> DeleteFavoriteAsync(VideoInfo item)
    {
        await Init();
        return await _database.DeleteAsync(item);
    }

    public async Task<int> DeleteFavoriteByPathAsync(string filePath)
    {
        await Init();

        var itemToDelete = await GetFavoriteAsync(filePath);
        if (itemToDelete != null)
        {
            return await _database.DeleteAsync(itemToDelete);
        }

        return 0;
    }
}