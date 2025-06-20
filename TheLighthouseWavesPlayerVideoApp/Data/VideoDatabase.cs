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
        await _database.CreateTableAsync<Playlist>();
        await _database.CreateTableAsync<PlaylistItem>();
    }

    public async Task<List<VideoInfo>> GetFavoritesAsync()
    {
        await Init();
    
        return await _database.Table<VideoInfo>()
            .Where(v => v.IsFavorite)
            .ToListAsync();
    }

    public async Task<VideoInfo> GetFavoriteAsync(string filePath)
    {
        await Init();
        return await _database.Table<VideoInfo>().Where(i => i.FilePath == filePath).FirstOrDefaultAsync();
    }
    
    public async Task<int> DeleteFavoriteAsync(VideoInfo item)
    {
        await Init();
        return await _database.DeleteAsync(item);
    }
  
    public async Task<int> UpdateVideoInfoAsync(VideoInfo item)
    {
        await Init();
    
        var existing = await GetFavoriteAsync(item.FilePath);
        if (existing == null)
        {
            return await _database.InsertAsync(item);
        }
        else
        {
            existing.Title = item.Title;
            existing.DurationMilliseconds = item.DurationMilliseconds;
            existing.ThumbnailPath = item.ThumbnailPath;
            existing.IsFavorite = item.IsFavorite;
            return await _database.UpdateAsync(existing);
        }
    }

    public async Task<int> SaveFavoriteAsync(VideoInfo item)
    {
        await Init();

        var existing = await GetFavoriteAsync(item.FilePath);
        if (existing == null)
        {
            return await _database.InsertAsync(item);
        }
        else
        {
            existing.Title = item.Title;
            existing.DurationMilliseconds = item.DurationMilliseconds;
            existing.ThumbnailPath = item.ThumbnailPath;
            existing.IsFavorite = item.IsFavorite;
            return await _database.UpdateAsync(existing);
        }
    }
    
    public async Task<int> DeleteFavoriteByPathAsync(string filePath)
    {
        await Init();

        var item = await GetFavoriteAsync(filePath);
        if (item != null)
        {
            item.IsFavorite = false;
            return await _database.UpdateAsync(item);
        }

        return 0;
    }
    
     public async Task<List<Playlist>> GetPlaylistsAsync()
    {
        await Init();
        return await _database.Table<Playlist>()
            .OrderByDescending(p => p.LastModified)
            .ToListAsync();
    }

    public async Task<Playlist> GetPlaylistAsync(int playlistId)
    {
        await Init();
        return await _database.Table<Playlist>()
            .Where(p => p.Id == playlistId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SavePlaylistAsync(Playlist playlist)
    {
        await Init();
        
        if (playlist.Id != 0)
        {
            playlist.LastModified = DateTime.Now;
            return await _database.UpdateAsync(playlist);
        }
        else
        {
            playlist.CreatedDate = DateTime.Now;
            playlist.LastModified = DateTime.Now;
            return await _database.InsertAsync(playlist);
        }
    }

    public async Task<int> DeletePlaylistAsync(Playlist playlist)
    {
        await Init();
        
        await _database.ExecuteAsync("DELETE FROM playlist_items WHERE PlaylistId = ?", playlist.Id);
        
        return await _database.DeleteAsync(playlist);
    }

    public async Task<List<PlaylistItem>> GetPlaylistItemsAsync(int playlistId)
    {
        await Init();
        return await _database.Table<PlaylistItem>()
            .Where(pi => pi.PlaylistId == playlistId)
            .OrderBy(pi => pi.Order)
            .ToListAsync();
    }

    public async Task<int> AddVideoToPlaylistAsync(int playlistId, string videoPath)
    {
        await Init();
        
        var existingItem = await _database.Table<PlaylistItem>()
            .Where(pi => pi.PlaylistId == playlistId && pi.VideoPath == videoPath)
            .FirstOrDefaultAsync();
            
        if (existingItem != null)
            return 0;
            
        var maxOrder = await _database.Table<PlaylistItem>()
            .Where(pi => pi.PlaylistId == playlistId)
            .OrderByDescending(pi => pi.Order)
            .FirstOrDefaultAsync();
            
        var newItem = new PlaylistItem
        {
            PlaylistId = playlistId,
            VideoPath = videoPath,
            Order = maxOrder?.Order + 1 ?? 0,
            AddedDate = DateTime.Now
        };
        
        var result = await _database.InsertAsync(newItem);
        
        var playlist = await GetPlaylistAsync(playlistId);
        if (playlist != null)
        {
            playlist.LastModified = DateTime.Now;
            await _database.UpdateAsync(playlist);
        }
        
        return result;
    }

    public async Task<int> RemoveVideoFromPlaylistAsync(int playlistId, string videoPath)
    {
        await Init();
        
        var item = await _database.Table<PlaylistItem>()
            .Where(pi => pi.PlaylistId == playlistId && pi.VideoPath == videoPath)
            .FirstOrDefaultAsync();
            
        if (item == null)
            return 0;
            
        var result = await _database.DeleteAsync(item);
        
        var remainingItems = await GetPlaylistItemsAsync(playlistId);
        for (int i = 0; i < remainingItems.Count; i++)
        {
            remainingItems[i].Order = i;
            await _database.UpdateAsync(remainingItems[i]);
        }
        
        var playlist = await GetPlaylistAsync(playlistId);
        if (playlist != null)
        {
            playlist.LastModified = DateTime.Now;
            await _database.UpdateAsync(playlist);
        }
        
        return result;
    }

    public async Task<int> ReorderPlaylistItemAsync(int itemId, int newOrder)
    {
        await Init();
        
        var item = await _database.Table<PlaylistItem>()
            .Where(pi => pi.Id == itemId)
            .FirstOrDefaultAsync();
            
        if (item == null)
            return 0;
            
        item.Order = newOrder;
        return await _database.UpdateAsync(item);
    }

    public async Task<int> UpdatePlaylistItemOrderAsync(List<PlaylistItem> items)
    {
        await Init();
        
        var result = 0;
        foreach (var item in items)
        {
            result += await _database.UpdateAsync(item);
        }
        
        return result;
    }
    
    public async Task<VideoInfo> GetOrCreateVideoInfoAsync(string filePath, VideoInfo videoInfo = null)
    {
        await Init();
    
        var existingVideo = await GetFavoriteAsync(filePath);
        if (existingVideo != null)
        {
            return existingVideo;
        }
    
        if (videoInfo == null)
        {
            return null;
        }
    
        var newVideoInfo = new VideoInfo
        {
            FilePath = filePath,
            Title = videoInfo.Title,
            DurationMilliseconds = videoInfo.DurationMilliseconds,
            ThumbnailPath = videoInfo.ThumbnailPath,
            IsFavorite = false
        };
    
        await _database.InsertAsync(newVideoInfo);
        return newVideoInfo;
    }
}