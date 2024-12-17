using TheLighthouseWavesPlayerApp2.Models;

namespace TheLighthouseWavesPlayerApp2.Services.Interfaces;

public interface IBookmarkService
{
    Task<int> AddBookmarkAsync(Bookmark bookmark);
    Task<List<Bookmark>> GetBookmarksForVideoAsync(int videoId);
    Task<int> DeleteBookmarkAsync(int id);
}