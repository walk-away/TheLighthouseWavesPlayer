using SQLite;
using TheLighthouseWavesPlayer.Core.Models;
using TheLighthouseWavesPlayerApp.Data;
using TheLighthouseWavesPlayerApp.Repositories.Interfaces;

namespace TheLighthouseWavesPlayerApp.Repositories;

public class PlaybackHistoryRepository : IPlaybackHistoryRepository
{
    private readonly SQLiteAsyncConnection _database;

    public PlaybackHistoryRepository()
    {
        _database = DatabaseProvider.DatabaseAsync;
    }

    public async Task<IEnumerable<PlaybackHistory>> GetPlaybackHistoryAsync() =>
        await _database.Table<PlaybackHistory>().ToListAsync();

    public async Task AddPlaybackHistoryAsync(PlaybackHistory playbackHistory) =>
        await _database.InsertAsync(playbackHistory);
}