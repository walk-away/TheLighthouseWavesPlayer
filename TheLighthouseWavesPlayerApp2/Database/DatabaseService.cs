using Microsoft.Extensions.Logging;
using SQLite;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.Database;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly ILogger<DatabaseService> _logger;
    private readonly IMemoryCacheService _cache;

    public DatabaseService(string dbPath, ILogger<DatabaseService> logger, IMemoryCacheService cache)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        _logger = logger;
        _cache = cache;
        InitializeAsync().Wait();
    }

    private async Task InitializeAsync()
    {
        await _database.CreateTableAsync<VideoItem>();
        await _database.CreateTableAsync<Bookmark>();
        await _database.CreateTableAsync<Playlist>();
        await _database.CreateTableAsync<PlaylistVideo>();
        await _database.CreateTableAsync<UserSettings>();
    }
    
    public async Task<int> AddVideoAsync(VideoItem video)
    {
        return await _database.InsertAsync(video);
    }

    public async Task<List<VideoItem>> GetVideosAsync()
    {
        return await _database.Table<VideoItem>().ToListAsync();
    }

    public async Task<VideoItem> GetVideoByIdAsync(int id)
    {
        return await _database.FindAsync<VideoItem>(id);
    }

    public async Task<int> UpdateVideoAsync(VideoItem video)
    {
        return await _database.UpdateAsync(video);
    }

    public async Task<int> DeleteVideoAsync(int id)
    {
        return await _database.DeleteAsync<VideoItem>(id);
    }

    public async Task<List<VideoItem>> GetFavoriteVideosAsync()
    {
        return await _database.Table<VideoItem>().Where(v => v.IsFavorite).ToListAsync();
    }

    public async Task<List<VideoItem>> GetRatedVideosAsync(int minRating)
    {
        return await _database.Table<VideoItem>().Where(v => v.Rating >= minRating).ToListAsync();
    }
    
    public async Task<int> AddBookmarkAsync(Bookmark bookmark)
    {
        return await _database.InsertAsync(bookmark);
    }

    public async Task<List<Bookmark>> GetBookmarksForVideoAsync(int videoId)
    {
        return await _database.Table<Bookmark>().Where(b => b.VideoItemId == videoId).ToListAsync();
    }

    public async Task<int> DeleteBookmarkAsync(int id)
    {
        return await _database.DeleteAsync<Bookmark>(id);
    }
    
    public async Task<int> AddPlaylistAsync(Playlist playlist)
    {
        return await _database.InsertAsync(playlist);
    }

    public async Task<List<Playlist>> GetPlaylistsAsync()
    {
        return await _database.Table<Playlist>().ToListAsync();
    }

    public async Task<Playlist> GetPlaylistByIdAsync(int id)
    {
        return await _database.FindAsync<Playlist>(id);
    }

    public async Task<int> UpdatePlaylistAsync(Playlist playlist)
    {
        return await _database.UpdateAsync(playlist);
    }

    public async Task<int> DeletePlaylistAsync(int id)
    {
        await _database.ExecuteAsync($"DELETE FROM PlaylistVideo WHERE PlaylistId = ?", id);
        return await _database.DeleteAsync<Playlist>(id);
    }
    
    public async Task<int> AddVideoToPlaylistAsync(int playlistId, int videoId)
    {
        return await _database.InsertAsync(new PlaylistVideo { PlaylistId = playlistId, VideoId = videoId });
    }

    public async Task<List<VideoItem>> GetVideosInPlaylistAsync(int playlistId)
    {
        var playlistVideos = await _database.Table<PlaylistVideo>()
            .Where(pv => pv.PlaylistId == playlistId)
            .ToListAsync();
        
        var videoIds = playlistVideos.Select(pv => pv.VideoId).ToList();
    
        return await _database.Table<VideoItem>()
            .Where(v => videoIds.Contains(v.Id))
            .ToListAsync();
    }

    public async Task<int> RemoveVideoFromPlaylistAsync(int playlistId, int videoId)
    {
        var entry = await _database.Table<PlaylistVideo>()
            .FirstOrDefaultAsync(pv => pv.PlaylistId == playlistId && pv.VideoId == videoId);
        if (entry != null)
        {
            return await _database.DeleteAsync<PlaylistVideo>(entry.Id);
        }
        return 0;
    }
    
    public async Task<UserSettings> GetUserSettingsAsync()
    {
        var settings = await _database.Table<UserSettings>().FirstOrDefaultAsync();
        return settings ?? new UserSettings
        {
            IsAutoplayEnabled = false,
            IsInfinitePlaybackEnabled = false,
            AspectMode = "AspectFit",
            Brightness = 1.0
        };
    }

    public async Task<int> SaveUserSettingsAsync(UserSettings settings)
    {
        var existingSettings = await _database.Table<UserSettings>().FirstOrDefaultAsync();
        if (existingSettings != null)
        {
            settings.Id = existingSettings.Id;
            return await _database.UpdateAsync(settings);
        }
        return await _database.InsertAsync(settings);
    }
    
    public async Task<List<VideoItem>> SearchVideosAsync(string searchTerm)
    {
        return await _database.Table<VideoItem>()
            .Where(v => !v.IsDeleted && (v.Title.Contains(searchTerm)))
            .ToListAsync();
    }

    public async Task<(List<VideoItem> Items, int TotalCount)> GetVideosPaginatedAsync(int page, int pageSize)
    {
        var query = _database.Table<VideoItem>().Where(v => !v.IsDeleted);
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<int> BulkAddVideosAsync(List<VideoItem> videos)
    {
        return await _database.InsertAllAsync(videos);
    }

    public async Task<bool> ExecuteInTransactionAsync(Func<Task> action)
    {
        try
        {
            await _database.RunInTransactionAsync(async (conn) =>
            {
                await action();
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing transaction");
            return false;
        }
    }

    public async Task<List<VideoItem>> GetVideosWithBookmarksAsync()
    {
        var cacheKey = "VideosWithBookmarks";
        
        if (_cache.TryGetValue(cacheKey, out List<VideoItem> videos))
            return videos;

        var allVideos = await GetVideosAsync();
        foreach (var video in allVideos)
        {
            video.Bookmarks = await GetBookmarksForVideoAsync(video.Id);
        }

        _cache.Set(cacheKey, allVideos, TimeSpan.FromMinutes(5));
        return allVideos;
    }

    public async Task<int> SoftDeleteVideoAsync(int id)
    {
        var video = await GetVideoByIdAsync(id);
        if (video != null)
        {
            video.IsDeleted = true;
            video.UpdatedAt = DateTime.UtcNow;
            return await UpdateVideoAsync(video);
        }
        return 0;
    }
}