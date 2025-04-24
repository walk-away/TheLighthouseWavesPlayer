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
    private readonly IScreenshotService _screenshotService;
    private const string PositionPreferenceKeyPrefix = "lastpos_";
    private List<SubtitleItem> _subtitles = new List<SubtitleItem>();
    [ObservableProperty] VideoMetadata videoInfo = new VideoMetadata();
    [ObservableProperty] string filePath;
    [ObservableProperty] MediaSource videoSource;
    [ObservableProperty] MediaElementState currentState = MediaElementState.None;
    [ObservableProperty] bool isFavorite;
    [ObservableProperty] bool shouldResumePlayback = false;
    [ObservableProperty] TimeSpan resumePosition = TimeSpan.Zero;
    [ObservableProperty] string currentSubtitleText = string.Empty;
    [ObservableProperty] bool hasSubtitles = false;
    [ObservableProperty] bool areSubtitlesEnabled = true;

    public VideoPlayerViewModel(IFavoritesService favoritesService, ISubtitleService subtitleService,
        IScreenshotService screenshotService)
    {
        _favoritesService = favoritesService;
        _subtitleService = subtitleService;
        _screenshotService = screenshotService;
        Title = "Player";
    }

    public void UpdateVideoMetadata(double width, double height, TimeSpan duration)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            System.Diagnostics.Debug.WriteLine("UpdateVideoMetadata skipped: FilePath is empty.");
            return;
        }

        if (width <= 0 || height <= 0 || duration <= TimeSpan.Zero)
        {
            System.Diagnostics.Debug.WriteLine(
                $"UpdateVideoMetadata skipped: Invalid dimensions or duration (W:{width}, H:{height}, D:{duration}). Waiting for valid data.");
            return;
        }

        try
        {
            var fileInfo = new FileInfo(FilePath);
            VideoInfo = new VideoMetadata
            {
                FileName = Path.GetFileName(FilePath),
                FilePath = FilePath,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                Resolution = $"{width}x{height}",
                Duration = duration
            };

            System.Diagnostics.Debug.WriteLine(
                $"Video metadata updated: {VideoInfo.FileName}, {VideoInfo.Resolution}, {VideoInfo.FormattedFileSize}, Duration: {VideoInfo.Duration}");
        }
        catch (FileNotFoundException fnfEx)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading video metadata (File Not Found): {fnfEx.Message}");
            VideoInfo = new VideoMetadata { FileName = "Error: File not found" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading video metadata: {ex.Message}");
            VideoInfo = new VideoMetadata { FileName = "Error loading metadata" };
        }
    }

    [ObservableProperty] bool isVideoInfoVisible = false;

    [RelayCommand]
    void ToggleVideoInfo()
    {
        IsVideoInfoVisible = !IsVideoInfoVisible;
    }

    [RelayCommand]
    async Task CaptureScreenshot()
    {
        if (MediaElement == null)
        {
            await Shell.Current.DisplayAlert("Error", "Media player not ready.", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            string filePathOrUri = await _screenshotService.CaptureScreenshotAsync(MediaElement);

            await Shell.Current.DisplayAlert("Success",
                $"Screenshot saved to gallery", "OK");
        }
        catch (UnauthorizedAccessException authEx)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot permission error: {authEx.Message}");
            await Shell.Current.DisplayAlert("Permission Error",
                "Storage permission is required to save screenshots to the gallery. Please grant permission in app settings.",
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot error: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to take screenshot.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ObservableProperty] private MediaElement mediaElement;

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