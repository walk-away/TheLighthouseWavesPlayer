using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Data;

public interface IDatabaseService
{
    Task<Video> GetByIdAsync(int id);
    Task<Video> GetVideoByPathAsync(string path);
    Task<IEnumerable<Video>> GetVideosAsync();
    Task<IEnumerable<Video>> GetFavoriteVideosAsync();
    Task AddVideoAsync(Video video);
    Task UpdateVideoAsync(Video video);
    Task DeleteVideoAsync(Video video);
    Task ToggleFavoriteAsync(int videoId);
}