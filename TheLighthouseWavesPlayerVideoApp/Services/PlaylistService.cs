using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class PlaylistService : IPlaylistService
{
    private readonly IVideoDatabase _videoDatabase;

    public PlaylistService(IVideoDatabase videoDatabase)
    {
        _videoDatabase = videoDatabase ?? throw new ArgumentNullException(nameof(videoDatabase));
    }

    public async Task<List<Playlist>> GetPlaylistsAsync()
    {
        var playlists = await _videoDatabase.GetPlaylistsAsync() ?? new List<Playlist>();
        foreach (var playlist in playlists)
        {
            await UpdatePlaylistStatisticsAsync(playlist);
        }
        return playlists;
    }

    public async Task<Playlist?> GetPlaylistAsync(int playlistId)
    {
        var playlist = await _videoDatabase.GetPlaylistAsync(playlistId);
        if (playlist != null)
        {
            await UpdatePlaylistStatisticsAsync(playlist);
        }
        return playlist;
    }

    public async Task<int> CreatePlaylistAsync(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Playlist name cannot be empty.", nameof(name));
        var playlist = new Playlist
        {
            Name = name,
            CreatedDate = DateTime.Now,
            LastModified = DateTime.Now
        };
        return await _videoDatabase.SavePlaylistAsync(playlist);
    }

    public async Task<int> UpdatePlaylistAsync(Playlist playlist)
    {
        if (playlist == null) throw new ArgumentNullException(nameof(playlist));
        playlist.LastModified = DateTime.Now;
        await UpdatePlaylistStatisticsAsync(playlist);
        return await _videoDatabase.SavePlaylistAsync(playlist);
    }

    public Task<int> DeletePlaylistAsync(Playlist playlist)
    {
        if (playlist == null) throw new ArgumentNullException(nameof(playlist));
        return _videoDatabase.DeletePlaylistAsync(playlist);
    }

    public async Task<List<VideoInfo>> GetPlaylistVideosAsync(int playlistId)
    {
        var result = new List<VideoInfo>();
        var playlistItems = await _videoDatabase.GetPlaylistItemsAsync(playlistId) ?? new List<PlaylistItem>();
        foreach (var item in playlistItems)
        {
            var videoInfo = await _videoDatabase.GetOrCreateVideoInfoAsync(item.VideoPath);
            if (videoInfo != null)
            {
                item.VideoInfo = videoInfo;
                result.Add(videoInfo);
            }
        }
        return result;
    }

    public async Task<int> AddVideoToPlaylistAsync(int playlistId, VideoInfo video)
    {
        if (video == null) throw new ArgumentNullException(nameof(video));
        var videoCopy = new VideoInfo
        {
            FilePath = video.FilePath,
            Title = video.Title,
            DurationMilliseconds = video.DurationMilliseconds,
            ThumbnailPath = video.ThumbnailPath,
            IsFavorite = video.IsFavorite
        };
        await _videoDatabase.GetOrCreateVideoInfoAsync(video.FilePath, videoCopy);
        var result = await _videoDatabase.AddVideoToPlaylistAsync(playlistId, video.FilePath);

        var playlist = await _videoDatabase.GetPlaylistAsync(playlistId);
        if (playlist != null)
        {
            await UpdatePlaylistStatisticsAsync(playlist);
            await _videoDatabase.SavePlaylistAsync(playlist);
        }
        return result;
    }

    public async Task UpdatePlaylistStatisticsAsync(Playlist playlist)
    {
        var items = await _videoDatabase.GetPlaylistItemsAsync(playlist.Id) ?? new List<PlaylistItem>();
        long totalDuration = 0;
        int validVideos = 0;
        foreach (var item in items)
        {
            var videoInfo = await _videoDatabase.GetOrCreateVideoInfoAsync(item.VideoPath);
            if (videoInfo != null)
            {
                totalDuration += videoInfo.DurationMilliseconds;
                validVideos++;
            }
        }
        playlist.VideoCount = validVideos;
        playlist.DisplayVideoCount = validVideos;
        var duration = TimeSpan.FromMilliseconds(totalDuration);
        playlist.TotalDuration = duration;
        playlist.DisplayTotalDuration = duration;
    }

    public async Task<int> RemoveVideoFromPlaylistAsync(int playlistId, string videoPath)
    {
        var result = await _videoDatabase.RemoveVideoFromPlaylistAsync(playlistId, videoPath);
        var playlist = await _videoDatabase.GetPlaylistAsync(playlistId);
        if (playlist != null)
        {
            await UpdatePlaylistStatisticsAsync(playlist);
            await _videoDatabase.SavePlaylistAsync(playlist);
        }
        return result;
    }

    public async Task<int> ReorderPlaylistAsync(int playlistId, List<PlaylistItem> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Order = i;
        }
        var result = await _videoDatabase.UpdatePlaylistItemOrderAsync(items);
        var playlist = await _videoDatabase.GetPlaylistAsync(playlistId);
        if (playlist != null)
        {
            await UpdatePlaylistStatisticsAsync(playlist);
            await _videoDatabase.SavePlaylistAsync(playlist);
        }
        return result;
    }

    public async Task<bool> IsVideoInPlaylistAsync(int playlistId, string videoFilePath)
    {
        var items = await _videoDatabase.GetPlaylistItemsAsync(playlistId) ?? new List<PlaylistItem>();
        return items.Any(item => item.VideoPath == videoFilePath);
    }
}