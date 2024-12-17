using TheLighthouseWavesPlayerApp2.Models;

namespace TheLighthouseWavesPlayerApp2.Services.Interfaces;

public interface IVideoService
{
    Task<int> AddVideoAsync(VideoItem video);
    Task<List<VideoItem>> GetVideosAsync();
    Task<VideoItem> GetVideoByIdAsync(int id);
    Task<int> UpdateVideoAsync(VideoItem video);
    Task<int> DeleteVideoAsync(int id);
    Task<int> SoftDeleteVideoAsync(int id);
    Task<List<VideoItem>> GetFavoriteVideosAsync();
    Task<List<VideoItem>> GetRatedVideosAsync(int minRating);
    Task<List<VideoItem>> SearchVideosAsync(string searchTerm);
    Task<(List<VideoItem> Items, int TotalCount)> GetVideosPaginatedAsync(int page, int pageSize);
    Task<List<VideoItem>> GetVideosWithBookmarksAsync();
    Task<int> BulkAddVideosAsync(List<VideoItem> videos);
}