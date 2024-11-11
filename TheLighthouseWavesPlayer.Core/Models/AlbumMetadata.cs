namespace TheLighthouseWavesPlayer.Core.Models;

public class AlbumMetadata
{
    public Guid AlbumMetadataId { get; set; } = Guid.NewGuid();
    public Guid AlbumId { get; set; }
    public string CoverArtUrl { get; set; }
    public string Description { get; set; }
    public string Label { get; set; }

    public Album Album { get; set; }

    public AlbumMetadata() { }

    public AlbumMetadata(Guid albumId, string coverArtUrl, string description, string label)
    {
        AlbumId = albumId;
        CoverArtUrl = coverArtUrl;
        Description = description;
        Label = label;
    }
}