using TheLighthouseWavesPlayer.Core.Enums;
using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.Repositories.Interfaces;

public interface IRatingRepository
{
    Task<Rating> GetRatingAsync(Guid itemId, RatingItemType itemType);
    Task AddOrUpdateRatingAsync(Rating rating);
    Task DeleteRatingAsync(Guid ratingId);
}