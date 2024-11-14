using SQLite;
using TheLighthouseWavesPlayer.Core.Enums;
using TheLighthouseWavesPlayer.Core.Models;
using TheLighthouseWavesPlayerApp.Data;
using TheLighthouseWavesPlayerApp.Repositories.Interfaces;

namespace TheLighthouseWavesPlayerApp.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly SQLiteAsyncConnection _database;

    public RatingRepository()
    {
        _database = DatabaseProvider.DatabaseAsync;
    }

    public async Task<Rating> GetRatingAsync(Guid itemId, RatingItemType itemType) =>
        await _database.Table<Rating>().Where(r => r.ItemId == itemId && r.ItemType == itemType).FirstOrDefaultAsync();

    public async Task AddOrUpdateRatingAsync(Rating rating)
    {
        var existingRating = await GetRatingAsync(rating.ItemId, rating.ItemType);
        if (existingRating != null)
        {
            existingRating.Score = rating.Score;
            await _database.UpdateAsync(existingRating);
        }
        else
        {
            await _database.InsertAsync(rating);
        }
    }

    public async Task DeleteRatingAsync(Guid ratingId)
    {
        var rating = await _database.FindAsync<Rating>(ratingId);
        if (rating != null)
        {
            await _database.DeleteAsync(rating);
        }
    }
}