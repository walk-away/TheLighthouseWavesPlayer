using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using TheLighthouseWavesPlayer.Core.Models.Explorer;

namespace TheLighthouseWavesPlayerApp;

public partial class MainPage : ContentPage
{
    private ObservableCollection<FileItem> _audioFiles = new();
    private MediaElement _mediaPlayer;
    private int _currentTrackIndex = -1;

    public MainPage()
    {
        InitializeComponent();
        var savedVolume = Preferences.Get("Volume", 0.5);
        _mediaPlayer = new MediaElement
        {
            Volume = savedVolume
        };
        VolumeSlider.Value = savedVolume;
    }

    private async void OnChooseFolderClicked(object sender, EventArgs e)
    {
        try
        {
            var folder = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (folder != null)
            {
                var folderInfo = new FolderInfo(folder.Folder.Path);
                _audioFiles = new ObservableCollection<FileItem>(
                    folderInfo.AudioFiles.Select(f => new FileItem(f)));
                AudioFilesList.ItemsSource = _audioFiles;
            }
            else
            {
                await DisplayAlert("Error", "No folder was selected.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private void OnAudioFileSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FileItem fileItem)
        {
            PlayAudio(fileItem.Path);
            _currentTrackIndex = _audioFiles.IndexOf(fileItem);
        }
    }

    private void OnPlayClicked(object sender, EventArgs e)
    {
        if (_currentTrackIndex >= 0)
        {
            PlayAudio(_audioFiles[_currentTrackIndex].Path);
        }
    }

    private void OnPrevTrackClicked(object sender, EventArgs e)
    {
        if (_currentTrackIndex > 0)
        {
            _currentTrackIndex--;
            PlayAudio(_audioFiles[_currentTrackIndex].Path);
        }
    }

    private void OnNextTrackClicked(object sender, EventArgs e)
    {
        if (_currentTrackIndex < _audioFiles.Count - 1)
        {
            _currentTrackIndex++;
            PlayAudio(_audioFiles[_currentTrackIndex].Path);
        }
    }

    private void PlayAudio(string filePath)
    {
        _mediaPlayer.Source = MediaSource.FromFile(filePath);
        _mediaPlayer.Play();
    }
    
    private void OnAudioItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (e.Item is FileItem fileItem)
        {
            PlayAudio(fileItem.Path);
            _currentTrackIndex = _audioFiles.IndexOf(fileItem);
        }
    }
    
    private void OnVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = e.NewValue;
            Preferences.Set("Volume", e.NewValue);
        }
    }
}