using TheLighthouseWavesPlayerApp2.Database;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.Services;

public class VideoService : IVideoService
{
    private readonly DatabaseService _database;

    public VideoService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<int> AddVideoAsync(VideoItem video)
    {
        return await _database.AddVideoAsync(video);
    }

    public async Task<List<VideoItem>> GetVideosAsync()
    {
        return await _database.GetVideosAsync();
    }

    public async Task<VideoItem> GetVideoByIdAsync(int id)
    {
        return await _database.GetVideoByIdAsync(id);
    }

    public async Task<int> UpdateVideoAsync(VideoItem video)
    {
        return await _database.UpdateVideoAsync(video);
    }

    public async Task<int> DeleteVideoAsync(int id)
    {
        return await _database.DeleteVideoAsync(id);
    }

    public async Task<int> SoftDeleteVideoAsync(int id)
    {
        return await _database.SoftDeleteVideoAsync(id);
    }

    public async Task<List<VideoItem>> GetFavoriteVideosAsync()
    {
        return await _database.GetFavoriteVideosAsync();
    }

    public async Task<List<VideoItem>> GetRatedVideosAsync(int minRating)
    {
        return await _database.GetRatedVideosAsync(minRating);
    }

    public async Task<List<VideoItem>> SearchVideosAsync(string searchTerm)
    {
        return await _database.SearchVideosAsync(searchTerm);
    }

    public async Task<(List<VideoItem> Items, int TotalCount)> GetVideosPaginatedAsync(int page, int pageSize)
    {
        return await _database.GetVideosPaginatedAsync(page, pageSize);
    }

    public async Task<List<VideoItem>> GetVideosWithBookmarksAsync()
    {
        return await _database.GetVideosWithBookmarksAsync();
    }

    public async Task<int> BulkAddVideosAsync(List<VideoItem> videos)
    {
        return await _database.BulkAddVideosAsync(videos);
    }
}
