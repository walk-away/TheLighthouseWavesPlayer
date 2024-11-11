using TheLighthouseWavesPlayer.Core.Enums;

namespace TheLighthouseWavesPlayer.Core.Models;

public class Rating
{
    public Guid RatingId { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public RatingItemType ItemType { get; set; }
    public int Score { get; set; } 
    public DateTime DateRated { get; set; } = DateTime.Now;

    public Album Album { get; set; }
    public Track Track { get; set; }

    public Rating() { }

    public Rating(Guid itemId, RatingItemType itemType, int score)
    {
        ItemId = itemId;
        ItemType = itemType;
        Score = score;
    }
}