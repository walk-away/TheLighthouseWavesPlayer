using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.ViewModels;

public class VideoViewModel : BaseViewModel
{
    private readonly IVideoService _videoService;
    private MediaElement _mediaElement;

    public ObservableCollection<VideoItem> Videos { get; } = new();

    public VideoViewModel(IVideoService videoService)
    {
        _videoService = videoService;
        LoadVideosCommand = new Command(async () => await LoadVideos());
        OpenFolderCommand = new Command(async () => await OpenFolderAndLoadFiles());
        TogglePlayCommand = new Command(TogglePlay);
        ForwardCommand = new Command(Forward);
        BackwardCommand = new Command(Backward);
    }

    public ICommand OpenFolderCommand { get; }
    public ICommand LoadVideosCommand { get; }
    public ICommand TogglePlayCommand { get; }
    public ICommand ForwardCommand { get; }
    public ICommand BackwardCommand { get; }

    public MediaElement MediaElement
    {
        get => _mediaElement;
        set => SetProperty(ref _mediaElement, value);
    }

    private VideoItem _selectedVideo;
    public VideoItem SelectedVideo
    {
        get => _selectedVideo;
        set
        {
            if (SetProperty(ref _selectedVideo, value) && _selectedVideo != null)
            {
                MediaElement.Source = _selectedVideo.FilePath;
                MediaElement.Play();
            }
        }
    }

    private async Task OpenFolderAndLoadFiles()
    {
        try
        {
            var result = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Choose videos",
                FileTypes = FilePickerFileType.Videos
            });

            if (result != null)
            {
                Videos.Clear();
                foreach (var file in result)
                {
                    Videos.Add(new VideoItem
                    {
                        Title = Path.GetFileNameWithoutExtension(file.FullPath),
                        FilePath = file.FullPath,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open files: {ex.Message}", "OK");
        }
    }

    private async Task LoadVideos()
    {
        var videos = await _videoService.GetVideosAsync();
        Videos.Clear();
        foreach (var video in videos)
        {
            Videos.Add(video);
        }
    }

    private void TogglePlay()
    {
        if (MediaElement != null)
        {
            if (MediaElement.CurrentState == MediaElementState.Playing)
                MediaElement.Pause();
            else
                MediaElement.Play();
        }
    }

    private void Forward()
    {
        if (MediaElement != null)
        {
            var newPosition = MediaElement.Position + TimeSpan.FromSeconds(10);
            MediaElement.SeekTo(newPosition);
        }
    }

    private void Backward()
    {
        if (MediaElement != null)
        {
            var newPosition = MediaElement.Position - TimeSpan.FromSeconds(10);
            MediaElement.SeekTo(newPosition);
        }
    }
}