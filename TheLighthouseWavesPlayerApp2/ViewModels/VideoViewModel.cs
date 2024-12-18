using System.Collections.ObjectModel;
using System.Windows.Input;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.ViewModels;

public class VideoViewModel : BaseViewModel
{
    private readonly IVideoService _videoService;

    public ObservableCollection<VideoItem> Videos { get; } = new();

    public VideoViewModel(IVideoService videoService)
    {
        _videoService = videoService;
        LoadVideosCommand = new Command(async () => await LoadVideos());
    }

    public ICommand LoadVideosCommand { get; }

    private async Task LoadVideos()
    {
        var videos = await _videoService.GetVideosAsync();
        Videos.Clear();
        foreach (var video in videos)
        {
            Videos.Add(video);
        }
    }
}