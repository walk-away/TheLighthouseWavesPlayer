using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

[QueryProperty(nameof(FilePath), "FilePath")]
[QueryProperty(nameof(PlaylistId), "PlaylistId")]
public partial class VideoPlayerViewModel : BaseViewModel, IDisposable
{
    private readonly IFavoritesService _favoritesService;
    private readonly ISubtitleService _subtitleService;
    private readonly IScreenshotService _screenshotService;
    private readonly IScreenWakeService _screenWakeService;
    private readonly ILocalizedResourcesProvider _resourcesProvider;
    private readonly IPlaylistService _playlistService;
    private const string PositionPreferenceKeyPrefix = "lastpos_";
    private List<SubtitleItem> _subtitles = new List<SubtitleItem>();
    private double _previousVolume = 0.5;
    private bool _isDisposed = false;
    private readonly SemaphoreSlim _metadataUpdateLock = new SemaphoreSlim(1, 1);
    private List<VideoInfo> _playlistVideos = new List<VideoInfo>();
    private List<VideoInfo> _playQueue = new List<VideoInfo>();
    private int _currentPlaylistIndex = -1;

    [ObservableProperty] VideoMetadata _videoInfo = new VideoMetadata();
    [ObservableProperty] string _filePath = "";
    [ObservableProperty] MediaSource? _videoSource;
    [ObservableProperty] MediaElementState _currentState = MediaElementState.None;
    [ObservableProperty] bool _isFavorite;
    [ObservableProperty] bool _shouldResumePlayback = false;
    [ObservableProperty] TimeSpan _resumePosition = TimeSpan.Zero;
    [ObservableProperty] string _currentSubtitleText = string.Empty;
    [ObservableProperty] bool _hasSubtitles = false;
    [ObservableProperty] bool _areSubtitlesEnabled = true;
    [ObservableProperty] private double _volumeAmplification = 2.0;
    [ObservableProperty] private double _sliderVolume = 0.5;
    [ObservableProperty] bool _isVideoInfoVisible = false;
    [ObservableProperty] private MediaElement? _mediaElement;
    [ObservableProperty] private bool _isReturningFromNavigation;
    [ObservableProperty] private TimeSpan _lastKnownPosition = TimeSpan.Zero;
    [ObservableProperty] private bool _isLandscape;
    [ObservableProperty] private int _playlistId;
    [ObservableProperty] private Playlist _currentPlaylist = new Playlist();
    [ObservableProperty] private string _playlistProgressText = string.Empty;
    [ObservableProperty] private bool _isPlaylistMode = false;
    [ObservableProperty] private bool _canGoToNext = false;
    [ObservableProperty] private bool _canGoToPrevious = false;
    [ObservableProperty] private bool _isPlaylistControlsVisible = false;
    [ObservableProperty] private bool _isShuffleEnabled = false;
    [ObservableProperty] private RepeatMode _repeatMode = RepeatMode.None;
    [ObservableProperty] private string _repeatModeIcon = "🔁";

    public VideoPlayerViewModel(
        IFavoritesService favoritesService,
        ISubtitleService subtitleService,
        IScreenshotService screenshotService,
        IScreenWakeService screenWakeService,
        ILocalizedResourcesProvider resourcesProvider,
        IPlaylistService playlistService)
    {
        _favoritesService = favoritesService;
        _subtitleService = subtitleService;
        _screenshotService = screenshotService;
        _screenWakeService = screenWakeService;
        _resourcesProvider = resourcesProvider;
        _playlistService = playlistService;
        Title = _resourcesProvider["Player_Title"];

        if (resourcesProvider is ObservableObject observableProvider)
        {
            observableProvider.PropertyChanged += OnResourceProviderPropertyChanged;
        }
    }

    partial void OnPlaylistIdChanged(int value)
    {
        if (value > 0)
        {
            IsPlaylistMode = true;
            Task.Run(async () => await InitializePlaylistAsync());
        }
        else
        {
            IsPlaylistMode = false;
            IsPlaylistControlsVisible = false;
        }
    }

    partial void OnIsPlaylistModeChanged(bool value)
    {
        IsPlaylistControlsVisible = value && _playlistVideos.Count > 1;
    }

    partial void OnRepeatModeChanged(RepeatMode value)
    {
        switch (value)
        {
            case RepeatMode.None:
                RepeatModeIcon = "⤵";
                break;
            case RepeatMode.All:
                RepeatModeIcon = "🔁";
                break;
            case RepeatMode.One:
                RepeatModeIcon = "🔂";
                break;
        }

        UpdateNavigationButtons();
    }

    private void OnResourceProviderPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item")
        {
            Title = _resourcesProvider["Player_Title"];
        }
    }

    private async Task InitializePlaylistAsync()
    {
        if (PlaylistId <= 0) return;

        try
        {
            MainThread.BeginInvokeOnMainThread(() => IsBusy = true);

            CurrentPlaylist = await _playlistService.GetPlaylistAsync(PlaylistId);
            if (CurrentPlaylist == null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert(
                        _resourcesProvider["PlaylistPlayer_Error"] ?? "Ошибка",
                        _resourcesProvider["PlaylistPlayer_PlaylistNotFound"] ?? "Плейлист не найден",
                        _resourcesProvider["Button_OK"] ?? "ОК");
                    await Shell.Current.GoToAsync("..");
                });
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => Title = CurrentPlaylist.Name);

            _playlistVideos = await _playlistService.GetPlaylistVideosAsync(PlaylistId);
            if (_playlistVideos.Count == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert(
                        _resourcesProvider["PlaylistPlayer_EmptyPlaylist"] ?? "Пустой плейлист",
                        _resourcesProvider["PlaylistPlayer_NoVideos"] ?? "В этом плейлисте нет видео",
                        _resourcesProvider["Button_OK"] ?? "ОК");
                    await Shell.Current.GoToAsync("..");
                });
                return;
            }

            _playQueue = new List<VideoInfo>(_playlistVideos);

            _currentPlaylistIndex = 0;
            var firstVideo = _playQueue[_currentPlaylistIndex];

            MainThread.BeginInvokeOnMainThread(() =>
            {
                FilePath = firstVideo.FilePath;
                UpdateProgressText();
                UpdateNavigationButtons();
                IsPlaylistControlsVisible = _playQueue.Count > 1;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing playlist: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.DisplayAlert(
                    _resourcesProvider["PlaylistPlayer_Error"] ?? "Ошибка",
                    _resourcesProvider["PlaylistPlayer_LoadError"] ?? "Не удалось загрузить плейлист",
                    _resourcesProvider["Button_OK"] ?? "ОК");
                await Shell.Current.GoToAsync("..");
            });
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() => IsBusy = false);
        }
    }

    private void UpdateProgressText()
    {
        if (!IsPlaylistMode || _currentPlaylistIndex < 0 || _currentPlaylistIndex >= _playQueue.Count)
            return;

        PlaylistProgressText =
            $"{_resourcesProvider["PlaylistPlayer_Video"] ?? "Видео"} {_currentPlaylistIndex + 1} {_resourcesProvider["PlaylistPlayer_Of"] ?? "из"} {_playQueue.Count}";
    }

    private void UpdateNavigationButtons()
    {
        if (!IsPlaylistMode)
        {
            CanGoToNext = false;
            CanGoToPrevious = false;
            return;
        }

        if (RepeatMode == RepeatMode.All)
        {
            CanGoToNext = _playQueue.Count > 1;
            CanGoToPrevious = _playQueue.Count > 1;
        }
        else
        {
            CanGoToNext = _currentPlaylistIndex < _playQueue.Count - 1;
            CanGoToPrevious = _currentPlaylistIndex > 0;
        }
    }

    [RelayCommand]
    private async Task NextVideo()
    {
        if (!IsPlaylistMode || !CanGoToNext) return;

        if (_currentPlaylistIndex < _playQueue.Count - 1)
        {
            _currentPlaylistIndex++;
        }
        else if (RepeatMode == RepeatMode.All)
        {
            _currentPlaylistIndex = 0;
        }
        else
        {
            return;
        }

        var nextVideo = _playQueue[_currentPlaylistIndex];
        FilePath = nextVideo.FilePath;

        UpdateProgressText();
        UpdateNavigationButtons();

        await Task.Delay(100);
        if (MediaElement != null)
        {
            MediaElement.Play();
        }
    }

    [RelayCommand]
    private async Task PreviousVideo()
    {
        if (!IsPlaylistMode || !CanGoToPrevious) return;

        if (_currentPlaylistIndex > 0)
        {
            _currentPlaylistIndex--;
        }
        else if (RepeatMode == RepeatMode.All)
        {
            _currentPlaylistIndex = _playQueue.Count - 1;
        }
        else
        {
            return;
        }

        var prevVideo = _playQueue[_currentPlaylistIndex];
        FilePath = prevVideo.FilePath;

        UpdateProgressText();
        UpdateNavigationButtons();
    }

    [RelayCommand]
    private void ToggleRepeatMode()
    {
        RepeatMode = RepeatMode switch
        {
            RepeatMode.None => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.None,
            _ => RepeatMode.None
        };
    }

    [RelayCommand]
    private void ToggleShuffle()
    {
        if (!IsPlaylistMode) return;

        IsShuffleEnabled = !IsShuffleEnabled;

        if (IsShuffleEnabled)
        {
            ShufflePlayQueue();
        }
        else
        {
            _playQueue = new List<VideoInfo>(_playlistVideos);

            if (_currentPlaylistIndex >= 0 && _currentPlaylistIndex < _playQueue.Count)
            {
                var currentVideo = _playQueue[_currentPlaylistIndex];
                _currentPlaylistIndex = _playlistVideos.IndexOf(currentVideo);
            }
        }

        UpdateNavigationButtons();
        UpdateProgressText();
    }

    private void ShufflePlayQueue()
    {
        VideoInfo? currentVideo = _currentPlaylistIndex >= 0 && _currentPlaylistIndex < _playQueue.Count
            ? _playQueue[_currentPlaylistIndex]
            : null;

        var shuffled = _playlistVideos.ToList();
        var random = new Random();

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        _playQueue = shuffled;

        if (currentVideo != null)
        {
            _currentPlaylistIndex = _playQueue.IndexOf(currentVideo);
        }
    }

    partial void OnSliderVolumeChanged(double value)
    {
        UpdateMediaElementVolume();
    }

    partial void OnVolumeAmplificationChanged(double value)
    {
        UpdateMediaElementVolume();
    }

    internal void UpdateMediaElementVolume()
    {
        if (MediaElement != null)
        {
            MediaElement.Volume = SliderVolume * VolumeAmplification;
        }
    }

    public async Task UpdateVideoMetadataAsync(double width, double height, TimeSpan duration)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            System.Diagnostics.Debug.WriteLine("UpdateVideoMetadata skipped: FilePath is empty.");
            return;
        }

        if (width <= 0 || height <= 0 || duration <= TimeSpan.Zero)
        {
            System.Diagnostics.Debug.WriteLine(
                $"UpdateVideoMetadata skipped: Invalid dimensions or duration (W:{width}, H:{height}, D:{duration}).");
            return;
        }

        await _metadataUpdateLock.WaitAsync();
        try
        {
            var fileInfo = new FileInfo(FilePath);
            VideoInfo = new VideoMetadata
            {
                FileName = Path.GetFileName(FilePath),
                FilePath = FilePath,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                Resolution = $"{(int)Math.Round(width)}x{(int)Math.Round(height)}",
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
        finally
        {
            _metadataUpdateLock.Release();
        }
    }

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
            await Shell.Current.DisplayAlert(
                _resourcesProvider["Player_Error_Title"],
                _resourcesProvider["Player_Error_Playback"],
                _resourcesProvider["Button_OK"]);
            return;
        }

        try
        {
            IsBusy = true;

            string filePathOrUri = await _screenshotService.CaptureScreenshotAsync(MediaElement);

            await Shell.Current.DisplayAlert(
                _resourcesProvider["Player_Screenshot_Success"],
                _resourcesProvider["Player_Screenshot_Success"],
                _resourcesProvider["Button_OK"]);
        }
        catch (UnauthorizedAccessException authEx)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot permission error: {authEx.Message}");
            await Shell.Current.DisplayAlert(
                _resourcesProvider["Player_Error_Title"],
                _resourcesProvider["Player_Permission_Error"],
                _resourcesProvider["Button_OK"]);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot error: {ex.Message}");
            await Shell.Current.DisplayAlert(
                _resourcesProvider["Player_Error_Title"],
                _resourcesProvider["Player_Screenshot_Error"],
                _resourcesProvider["Button_OK"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    async partial void OnFilePathChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            VideoSource = MediaSource.FromFile(value);
            if (!IsPlaylistMode)
            {
                Title = Path.GetFileName(value);
            }

            await CheckFavoriteStatusAsync();
            CheckForSavedPosition();
            await LoadSubtitlesAsync();
        }
        else
        {
            VideoSource = null;
            if (!IsPlaylistMode)
            {
                Title = _resourcesProvider["Player_Title"];
            }

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

        try
        {
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading subtitles: {ex.Message}");
            HasSubtitles = false;
            _subtitles.Clear();
        }
    }

    public void UpdateSubtitles(TimeSpan position)
    {
        if (!HasSubtitles || !AreSubtitlesEnabled || _subtitles.Count == 0)
        {
            if (CurrentSubtitleText != string.Empty)
                CurrentSubtitleText = string.Empty;
            return;
        }

        int left = 0;
        int right = _subtitles.Count - 1;
        SubtitleItem? activeSubtitle = null;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            var subtitle = _subtitles[mid];

            if (subtitle.IsActiveAt(position))
            {
                activeSubtitle = subtitle;
                break;
            }

            if (position < subtitle.StartTime)
            {
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        string newText = activeSubtitle?.Text ?? string.Empty;
        if (CurrentSubtitleText != newText)
        {
            CurrentSubtitleText = newText;
        }
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

    public void CheckForSavedPosition()
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
    void ToggleMute()
    {
        if (MediaElement == null) return;

        if (MediaElement.Volume > 0)
        {
            _previousVolume = SliderVolume;
            SliderVolume = 0;
        }
        else
        {
            SliderVolume = _previousVolume > 0 ? _previousVolume : 0.5;
        }

        UpdateMediaElementVolume();
    }

    [RelayCommand]
    async Task ToggleFavoriteAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        try
        {
            var video = new VideoInfo
            {
                FilePath = this.FilePath,
                Title = Path.GetFileNameWithoutExtension(this.FilePath),
                DurationMilliseconds = (long)VideoInfo.Duration.TotalMilliseconds
            };

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
            await Shell.Current.DisplayAlert(
                _resourcesProvider["Error_Title"],
                _resourcesProvider["Error_Favorites_Update"],
                _resourcesProvider["Button_OK"]);
        }
    }

    public void OnNavigatedFrom(TimeSpan currentPosition)
    {
        try
        {
            if ((CurrentState == MediaElementState.Playing || CurrentState == MediaElementState.Paused)
                && currentPosition > TimeSpan.Zero && !string.IsNullOrEmpty(FilePath))
            {
                LastKnownPosition = currentPosition;
                string key = PositionPreferenceKeyPrefix + FilePath;
                Preferences.Set(key, currentPosition.Ticks);
                ShouldResumePlayback = true;
                System.Diagnostics.Debug.WriteLine($"Position saved before navigation: {currentPosition}, Key: {key}");
            }
            else
            {
                LastKnownPosition = TimeSpan.Zero;
                ShouldResumePlayback = false;
            }

            CurrentState = MediaElementState.None;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnNavigatedFrom: {ex}");
        }
    }

    partial void OnCurrentStateChanged(MediaElementState value)
    {
        if (value == MediaElementState.Playing)
        {
            _screenWakeService.KeepScreenOn();
            System.Diagnostics.Debug.WriteLine("Media playing: Keeping screen on");
        }
        else if (value == MediaElementState.Stopped ||
                 value == MediaElementState.Paused ||
                 value == MediaElementState.Failed ||
                 value == MediaElementState.None)
        {
            _screenWakeService.AllowScreenSleep();
            System.Diagnostics.Debug.WriteLine("Media not playing: Allowing screen to sleep");
        }

        if ((value == MediaElementState.Paused || value == MediaElementState.Stopped) &&
            MediaElement?.Position > TimeSpan.Zero)
        {
            SavePosition(MediaElement.Position);
        }
    }

    public void OnMediaEnded()
    {
        System.Diagnostics.Debug.WriteLine("Media ended.");

        if (IsPlaylistMode)
        {
            switch (RepeatMode)
            {
                case RepeatMode.One:
                    if (MediaElement != null)
                    {
                        MediaElement.SeekTo(TimeSpan.Zero);
                        MediaElement.Play();
                    }

                    break;

                case RepeatMode.All:
                    if (_currentPlaylistIndex < _playQueue.Count - 1)
                    {
                        _currentPlaylistIndex++;
                    }
                    else
                    {
                        _currentPlaylistIndex = 0;
                    }

                    var nextVideo1 = _playQueue[_currentPlaylistIndex];
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        FilePath = nextVideo1.FilePath;
                        UpdateProgressText();
                        UpdateNavigationButtons();

                        Task.Delay(100).ContinueWith(_ =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                if (MediaElement != null)
                                {
                                    MediaElement.Play();
                                }
                            });
                        });
                    });
                    break;

                case RepeatMode.None:
                default:
                    if (_currentPlaylistIndex < _playQueue.Count - 1)
                    {
                        _currentPlaylistIndex++;
                        var nextVideo2 = _playQueue[_currentPlaylistIndex];
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            FilePath = nextVideo2.FilePath;
                            UpdateProgressText();
                            UpdateNavigationButtons();

                            Task.Delay(100).ContinueWith(_ =>
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    if (MediaElement != null)
                                    {
                                        MediaElement.Play();
                                    }
                                });
                            });
                        });
                    }

                    break;
            }
        }
        else
        {
            ClearSavedPosition();
        }
    }

    public void Cleanup()
    {
        if (_isDisposed) return;

        _screenWakeService.AllowScreenSleep();
        System.Diagnostics.Debug.WriteLine("ViewModel cleanup: Allowing screen to sleep");

        if (MediaElement?.Position > TimeSpan.Zero && !string.IsNullOrEmpty(FilePath))
        {
            SavePosition(MediaElement.Position);
        }

        MediaElement = null;
    }

    [RelayCommand]
    void GoBack()
    {
        Shell.Current.GoToAsync("..");
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        if (_resourcesProvider is ObservableObject observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
        }

        Cleanup();
        _metadataUpdateLock?.Dispose();

        _isDisposed = true;
    }
}