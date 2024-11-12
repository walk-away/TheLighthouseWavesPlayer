using System.Text.Json;
using TheLighthouseWavesPlayer.Core.Models;

namespace TheLighthouseWavesPlayerApp.AI;

public class AiService(IAiProvider aiProvider) : IAiService
{
    public Task<string?> GetInfoByTrack(Track track)
    {
        var generalPrompt = $"""
                             Tell me about this track '{track.Title}'.
                             This is a famous track, so you must know a lot about it.
                             Provide as detailed information as possible. The description for track must contain a lot of text.
                             The output must contain only the detailed information because I will parse it later. Do not include any information or formatting except description text.
                             """;

        return aiProvider.GetResponse(generalPrompt);
    }

    public Task<string?> GetInfoByAlbum(Album album)
    {
        var generalPrompt = $"""
                             Tell me about this album '{album.Title}'.
                             This is a famous album, so you must know a lot about it.
                             Provide as detailed information as possible. The description for album must contain a lot of text.
                             The output must contain only the detailed information because I will parse it later. Do not include any information or formatting except description text.
                             """;

        return aiProvider.GetResponse(generalPrompt);
    }

    public Task<string?> GetInfoByArtist(Artist artist)
    {
        var generalPrompt = $"""
                             Tell me about this artist '{artist.Name}'.
                             This is a famous artist, so you must know a lot about them.
                             Provide as detailed information as possible. The description for artist must contain a lot of text.
                             The output must contain only the detailed information because I will parse it later. Do not include any information or formatting except description text.
                             """;

        return aiProvider.GetResponse(generalPrompt);
    }
}