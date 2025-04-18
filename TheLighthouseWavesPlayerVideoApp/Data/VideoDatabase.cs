using SQLite;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Data;

public class VideoDatabase
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
        // Create table only if it doesn't exist
        await _database.CreateTableAsync<VideoInfo>();
    }

    public async Task<List<VideoInfo>> GetFavoritesAsync()
    {
        await Init();
        // In this setup, the VideoInfo table IS the favorites table
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
        // Check if it already exists
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
        // Need to fetch the item first to delete by primary key if VideoInfo isn't passed
        // Or use ExecuteAsync for more complex delete queries if needed.
        var itemToDelete = await GetFavoriteAsync(filePath);
        if(itemToDelete != null)
        {
            return await _database.DeleteAsync(itemToDelete);
        }
        return 0; // Not found
    }
}