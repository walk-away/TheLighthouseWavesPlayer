using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using Microsoft.Maui.Storage;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

[QueryProperty(nameof(FilePath), "FilePath")]
public partial class VideoPlayerViewModel : BaseViewModel
{
    private readonly IFavoritesService _favoritesService;
    private readonly ISubtitleService _subtitleService;
    private const string PositionPreferenceKeyPrefix = "lastpos_";
    private List<SubtitleItem> _subtitles = new List<SubtitleItem>();

    [ObservableProperty] string filePath;
    [ObservableProperty] MediaSource videoSource;
    [ObservableProperty] MediaElementState currentState = MediaElementState.None;
    [ObservableProperty] bool isFavorite;
    [ObservableProperty] bool shouldResumePlayback = false;
    [ObservableProperty] TimeSpan resumePosition = TimeSpan.Zero;
    [ObservableProperty] string currentSubtitleText = string.Empty;
    [ObservableProperty] bool hasSubtitles = false;
    [ObservableProperty] bool areSubtitlesEnabled = true;

    public VideoPlayerViewModel(IFavoritesService favoritesService, ISubtitleService subtitleService)
    {
        _favoritesService = favoritesService;
        _subtitleService = subtitleService;
        Title = "Player";
    }

    async partial void OnFilePathChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            VideoSource = MediaSource.FromFile(value);
            Title = Path.GetFileName(value);
            await CheckFavoriteStatusAsync();
            CheckForSavedPosition();
            await LoadSubtitlesAsync();
        }
        else
        {
            VideoSource = null;
            Title = "Player";
            IsFavorite = false;
            CurrentState = MediaElementState.None;
            ShouldResumePlayback = false;
            ResumePosition = TimeSpan.Zero;
            CurrentSubtitleText = string.Empty;
            HasSubtitles = false;
        }
    }

    private async Task LoadSubtitlesAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        string directory = Path.GetDirectoryName(FilePath) ?? string.Empty;
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(FilePath);
        string srtFilePath = Path.Combine(directory, $"{fileNameWithoutExt}.srt");

        _subtitles = await _subtitleService.LoadSubtitlesAsync(srtFilePath);
        HasSubtitles = _subtitles.Count > 0;

        if (HasSubtitles)
        {
            System.Diagnostics.Debug.WriteLine($"Loaded {_subtitles.Count} subtitles from {srtFilePath}");
        }
    }

    public void UpdateSubtitles(TimeSpan position)
    {
        if (!HasSubtitles || !AreSubtitlesEnabled)
        {
            CurrentSubtitleText = string.Empty;
            return;
        }

        var subtitle = _subtitles.FirstOrDefault(s => s.IsActiveAt(position));
        CurrentSubtitleText = subtitle?.Text ?? string.Empty;
    }

    [RelayCommand]
    void ToggleSubtitles()
    {
        AreSubtitlesEnabled = !AreSubtitlesEnabled;
        if (!AreSubtitlesEnabled)
        {
            CurrentSubtitleText = string.Empty;
        }
    }

    public async Task CheckFavoriteStatusAsync()
    {
        if (!string.IsNullOrEmpty(FilePath))
        {
            IsFavorite = await _favoritesService.IsFavoriteAsync(FilePath);
        }
        else
        {
            IsFavorite = false;
        }
    }

    private void CheckForSavedPosition()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        string key = PositionPreferenceKeyPrefix + FilePath;
        long savedTicks = Preferences.Get(key, 0L);

        if (savedTicks > 0)
        {
            ResumePosition = TimeSpan.FromTicks(savedTicks);
            ShouldResumePlayback = true;
            System.Diagnostics.Debug.WriteLine($"Found saved position for {FilePath}: {ResumePosition}");
        }
        else
        {
            ShouldResumePlayback = false;
            ResumePosition = TimeSpan.Zero;
        }
    }

    public void SavePosition(TimeSpan currentPosition)
    {
        if (string.IsNullOrEmpty(FilePath) || currentPosition <= TimeSpan.Zero) return;


        string key = PositionPreferenceKeyPrefix + FilePath;
        Preferences.Set(key, currentPosition.Ticks);
        System.Diagnostics.Debug.WriteLine($"Saved position for {FilePath}: {currentPosition}");
    }

    public void ClearSavedPosition()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        string key = PositionPreferenceKeyPrefix + FilePath;
        if (Preferences.ContainsKey(key))
        {
            Preferences.Remove(key);
            System.Diagnostics.Debug.WriteLine($"Cleared saved position for {FilePath}");
        }

        ShouldResumePlayback = false;
        ResumePosition = TimeSpan.Zero;
    }


    [RelayCommand]
    async Task ToggleFavoriteAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        try
        {
            var video = new VideoInfo
                { FilePath = this.FilePath, Title = Path.GetFileNameWithoutExtension(this.FilePath) };

            if (IsFavorite)
            {
                await _favoritesService.RemoveFavoriteAsync(video);
                IsFavorite = false;
            }
            else
            {
                await _favoritesService.AddFavoriteAsync(video);
                IsFavorite = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling favorite in player: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Could not update favorites.", "OK");
        }
    }

    public void OnNavigatedFrom(TimeSpan currentPosition)
    {
        if (CurrentState == MediaElementState.Playing || CurrentState == MediaElementState.Paused)
        {
            SavePosition(currentPosition);
        }

        VideoSource = null;
        CurrentState = MediaElementState.None;
        ShouldResumePlayback = false;
        System.Diagnostics.Debug.WriteLine("VideoPlayerViewModel.OnNavigatedFrom called.");
    }
}