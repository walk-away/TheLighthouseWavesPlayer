using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public interface IMediaService
{
    Task<List<Video>> GetVideosFromMediaStore();
    Task<Video> GetVideoById(string id);
    Task<bool> ImportExternalVideo();
}