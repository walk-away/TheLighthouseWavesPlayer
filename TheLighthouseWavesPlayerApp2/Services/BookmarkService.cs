using TheLighthouseWavesPlayerApp2.Database;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.Services;

public class BookmarkService : IBookmarkService
{
    private readonly DatabaseService _database;

    public BookmarkService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<int> AddBookmarkAsync(Bookmark bookmark)
    {
        return await _database.AddBookmarkAsync(bookmark);
    }

    public async Task<List<Bookmark>> GetBookmarksForVideoAsync(int videoId)
    {
        return await _database.GetBookmarksForVideoAsync(videoId);
    }

    public async Task<int> DeleteBookmarkAsync(int id)
    {
        return await _database.DeleteBookmarkAsync(id);
    }
}