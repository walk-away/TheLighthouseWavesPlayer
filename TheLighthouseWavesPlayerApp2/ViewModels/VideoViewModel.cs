using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.ViewModels;
/*
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
        PlayCommand = new Command(Play);
        PauseCommand = new Command(Pause);
        StopCommand = new Command(Stop);
        MuteCommand = new Command(Mute);
        DecreaseSpeedCommand = new Command(DecreaseSpeed);
        IncreaseSpeedCommand = new Command(IncreaseSpeed);
        DecreaseVolumeCommand = new Command(DecreaseVolume);
        IncreaseVolumeCommand = new Command(IncreaseVolume);
        ChangeSourceCommand = new Command(ChangeSource);
        LoadCustomSourceCommand = new Command<string>(LoadCustomSource);
        ChangeAspectCommand = new Command(ChangeAspect);
    }

    public ICommand OpenFolderCommand { get; }
    public ICommand LoadVideosCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand MuteCommand { get; }
    public ICommand DecreaseSpeedCommand { get; }
    public ICommand IncreaseSpeedCommand { get; }
    public ICommand DecreaseVolumeCommand { get; }
    public ICommand IncreaseVolumeCommand { get; }
    public ICommand ChangeSourceCommand { get; }
    public ICommand LoadCustomSourceCommand { get; }
    public ICommand ChangeAspectCommand { get; }

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
                    Debug.WriteLine($"Adding file: {file.FullPath}");
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
            Debug.WriteLine($"Error in OpenFolderAndLoadFiles: {ex}");
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

    public void Play() => MediaElement?.Play();
    public void Pause() => MediaElement?.Pause();
    public void Stop() => MediaElement?.Stop();
    public void Mute() => MediaElement.ShouldMute = !MediaElement.ShouldMute;

    public void DecreaseSpeed()
    {
        if (MediaElement != null && MediaElement.Speed > 0.5)
        {
            MediaElement.Speed -= 0.5;
        }
    }

    public void IncreaseSpeed()
    {
        if (MediaElement != null && MediaElement.Speed < 2.0)
        {
            MediaElement.Speed += 0.5;
        }
    }

    public void DecreaseVolume()
    {
        if (MediaElement != null && MediaElement.Volume > 0)
        {
            MediaElement.Volume = Math.Max(0, MediaElement.Volume - 0.1);
        }
    }

    public void IncreaseVolume()
    {
        if (MediaElement != null && MediaElement.Volume < 1)
        {
            MediaElement.Volume = Math.Min(1, MediaElement.Volume + 0.1);
        }
    }

    public void ChangeSource()
    {
        // Implement logic to change source
    }

    public void LoadCustomSource(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            MediaElement.Source = url;
        }
    }

    public void ChangeAspect()
    {
        // Implement logic to change aspect ratio
    }

    public void OnMediaOpened(object sender, EventArgs e)
    {
        // Handle media opened event
    }

    public void OnMediaEnded(object sender, EventArgs e)
    {
        // Handle media ended event
    }

    public void OnMediaFailed(object sender, MediaFailedEventArgs e)
    {
        // Handle media failed event
    }

    public void OnPositionChanged(object sender, MediaPositionChangedEventArgs e)
    {
        // Handle position changed event
    }

    public void OnStateChanged(object sender, MediaStateChangedEventArgs e)
    {
        // Handle state changed event
    }

    public void OnSeekCompleted(object sender, EventArgs e)
    {
        // Handle seek completed event
    }

    public void Slider_DragStarted(object sender, EventArgs e)
    {
        MediaElement?.Pause();
    }

    public async Task Slider_DragCompleted(object sender, EventArgs e)
    {
        if (sender is Slider slider)
        {
            var newValue = slider.Value;
            await MediaElement.SeekTo(TimeSpan.FromSeconds(newValue), CancellationToken.None);
            MediaElement.Play();
        }
    }
}
*/

public class VideoViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private MediaElement _mediaElement;
    private VideoItem _selectedVideo;
    
    public ObservableCollection<VideoItem> Videos { get; } = new();
    
    public VideoViewModel()
    {
        OpenFolderCommand = new Command(async () => await OpenFolderAndLoadFiles());
        PlayPauseCommand = new Command(PlayPause);
    }

    public ICommand OpenFolderCommand { get; }
    public ICommand PlayPauseCommand { get; }

    public MediaElement MediaElement
    {
        get => _mediaElement;
        set
        {
            _mediaElement = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MediaElement)));
        }
    }

    public VideoItem SelectedVideo
    {
        get => _selectedVideo;
        set
        {
            _selectedVideo = value;
            if (_selectedVideo != null)
            {
                MediaElement.Source = _selectedVideo.FilePath;
                MediaElement.Play();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedVideo)));
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
                        FilePath = file.FullPath
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void PlayPause()
    {
        if (MediaElement.CurrentState == MediaElementState.Playing)
            MediaElement.Pause();
        else
            MediaElement.Play();
    }
    
    
    public void OnMediaOpened(object sender, EventArgs e)
    {
        if (sender is MediaElement mediaElement)
        {
            // Update max duration for slider if needed
            mediaElement.Volume = 1.0; // Default volume
            Debug.WriteLine($"Media opened: Duration = {mediaElement.Duration}");
        }
    }

    public async void OnMediaEnded(object sender, EventArgs e)
    {
        if (sender is MediaElement mediaElement)
        {
            try
            {
                await mediaElement.SeekTo(TimeSpan.Zero, CancellationToken.None);
                mediaElement.Pause();
                Debug.WriteLine("Media playback ended and reset to start");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resetting media position: {ex.Message}");
            }
        }
    }
}