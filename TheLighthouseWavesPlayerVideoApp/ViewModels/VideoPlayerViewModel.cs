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
public sealed partial class VideoPlayerViewModel : BaseViewModel, IDisposable
{
    private readonly IFavoritesService _favoritesService;
    private readonly ISubtitleService _subtitleService;
    private readonly IScreenshotService _screenshotService;
    private readonly IScreenWakeService _screenWakeService;
    private readonly ILocalizedResourcesProvider _resourcesProvider;
    private readonly IPlaylistService _playlistService;
    private readonly IDialogService _dialogService;

    private const string PositionPreferenceKeyPrefix = "lastpos_";
    private List<SubtitleItem> _subtitles = new();
    private double _previousVolume = 0.5;
    private bool _isDisposed;
    private readonly SemaphoreSlim _metadataUpdateLock = new(1, 1);
    private readonly SemaphoreSlim _playlistLock = new(1, 1);
    private List<VideoInfo> _playlistVideos = new();
    private List<VideoInfo> _playQueue = new();
    private int _currentPlaylistIndex = -1;
    private CancellationTokenSource? _initializationCts;

    [ObservableProperty] private VideoMetadata _videoInfo = new();
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private MediaSource? _videoSource;
    [ObservableProperty] private MediaElementState _currentState = MediaElementState.None;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private bool _shouldResumePlayback;
    [ObservableProperty] private TimeSpan _resumePosition = TimeSpan.Zero;
    [ObservableProperty] private string _currentSubtitleText = string.Empty;
    [ObservableProperty] private bool _hasSubtitles;
    [ObservableProperty] private bool _areSubtitlesEnabled = true;
    [ObservableProperty] private double _volumeAmplification = 2.0;
    [ObservableProperty] private double _sliderVolume = 0.5;
    [ObservableProperty] private bool _isVideoInfoVisible;
    [ObservableProperty] private MediaElement? _mediaElement;
    [ObservableProperty] private bool _isReturningFromNavigation;
    [ObservableProperty] private TimeSpan _lastKnownPosition = TimeSpan.Zero;
    [ObservableProperty] private bool _isLandscape;
    [ObservableProperty] private int _playlistId;
    [ObservableProperty] private Playlist _currentPlaylist = new();
    [ObservableProperty] private string _playlistProgressText = string.Empty;
    [ObservableProperty] private bool _isPlaylistMode;
    [ObservableProperty] private bool _canGoToNext;
    [ObservableProperty] private bool _canGoToPrevious;
    [ObservableProperty] private bool _isPlaylistControlsVisible;
    [ObservableProperty] private bool _isShuffleEnabled;
    [ObservableProperty] private RepeatMode _repeatMode = RepeatMode.None;
    [ObservableProperty] private string _repeatModeIcon = "🔁";

    public event EventHandler<ResumePlaybackEventArgs>? ResumePlaybackRequested;
    public event EventHandler? PlayRequested;
    public event EventHandler<SeekEventArgs>? SeekRequested;

    public VideoPlayerViewModel(
        IFavoritesService favoritesService,
        ISubtitleService subtitleService,
        IScreenshotService screenshotService,
        IScreenWakeService screenWakeService,
        ILocalizedResourcesProvider resourcesProvider,
        IPlaylistService playlistService,
        IDialogService dialogService)
    {
        _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
        _subtitleService = subtitleService ?? throw new ArgumentNullException(nameof(subtitleService));
        _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        _screenWakeService = screenWakeService ?? throw new ArgumentNullException(nameof(screenWakeService));
        _resourcesProvider = resourcesProvider ?? throw new ArgumentNullException(nameof(resourcesProvider));
        _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

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
            _ = InitializePlaylistAsync();
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
        RepeatModeIcon = value switch
        {
            RepeatMode.None => "⤵",
            RepeatMode.All => "🔁",
            RepeatMode.One => "🔂",
            _ => "⤵"
        };
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

        if (_initializationCts != null)
        {
            await _initializationCts.CancelAsync();
        }

        _initializationCts = new CancellationTokenSource();
        var token = _initializationCts.Token;

        await _playlistLock.WaitAsync(token);
        try
        {
            IsBusy = true;

            var playlist = await _playlistService.GetPlaylistAsync(PlaylistId);
            if (playlist == null)
            {
                await _dialogService.ShowAlertAsync(
                    _resourcesProvider["PlaylistPlayer_Error"] ?? "Ошибка",
                    _resourcesProvider["PlaylistPlayer_PlaylistNotFound"] ?? "Плейлист не найден",
                    _resourcesProvider["Button_OK"] ?? "ОК");
                await _dialogService.NavigateBackAsync();
                return;
            }

            token.ThrowIfCancellationRequested();

            CurrentPlaylist = playlist;
            Title = CurrentPlaylist.Name;

            _playlistVideos = await _playlistService.GetPlaylistVideosAsync(PlaylistId);

            token.ThrowIfCancellationRequested();

            if (_playlistVideos.Count == 0)
            {
                await _dialogService.ShowAlertAsync(
                    _resourcesProvider["PlaylistPlayer_EmptyPlaylist"] ?? "Пустой плейлист",
                    _resourcesProvider["PlaylistPlayer_NoVideos"] ?? "В этом плейлисте нет видео",
                    _resourcesProvider["Button_OK"] ?? "ОК");
                await _dialogService.NavigateBackAsync();
                return;
            }

            _playQueue = new List<VideoInfo>(_playlistVideos);
            _currentPlaylistIndex = 0;

            var firstVideo = _playQueue[_currentPlaylistIndex];
            FilePath = firstVideo.FilePath;
            UpdateProgressText();
            UpdateNavigationButtons();
            IsPlaylistControlsVisible = _playQueue.Count > 1;
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Playlist initialization was cancelled.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing playlist: {ex.Message}");
            await _dialogService.ShowAlertAsync(
                _resourcesProvider["PlaylistPlayer_Error"] ?? "Ошибка",
                _resourcesProvider["PlaylistPlayer_LoadError"] ?? "Не удалось загрузить плейлист",
                _resourcesProvider["Button_OK"] ?? "ОК");
            await _dialogService.NavigateBackAsync();
        }
        finally
        {
            _playlistLock.Release();
            IsBusy = false;
        }
    }

    private void UpdateProgressText()
    {
        if (!IsPlaylistMode || _currentPlaylistIndex < 0 || _currentPlaylistIndex >= _playQueue.Count)
            return;

        PlaylistProgressText =
            $"{_resourcesProvider["PlaylistPlayer_Video"] ?? "Видео"} {_currentPlaylistIndex + 1} " +
            $"{_resourcesProvider["PlaylistPlayer_Of"] ?? "из"} {_playQueue.Count}";
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
    private async Task NextVideoAsync()
    {
        if (!IsPlaylistMode || !CanGoToNext) return;

        await _playlistLock.WaitAsync();
        try
        {
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

            await NavigateToCurrentVideoAsync();
        }
        finally
        {
            _playlistLock.Release();
        }
    }

    [RelayCommand]
    private async Task PreviousVideoAsync()
    {
        if (!IsPlaylistMode || !CanGoToPrevious) return;

        await _playlistLock.WaitAsync();
        try
        {
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

            await NavigateToCurrentVideoAsync();
        }
        finally
        {
            _playlistLock.Release();
        }
    }

    private async Task NavigateToCurrentVideoAsync()
    {
        var video = _playQueue[_currentPlaylistIndex];
        FilePath = video.FilePath;
        CheckForSavedPosition();
        UpdateProgressText();
        UpdateNavigationButtons();

        await Task.Delay(100);
        PlayRequested?.Invoke(this, EventArgs.Empty);
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
            RestoreOriginalOrder();
        }

        UpdateNavigationButtons();
        UpdateProgressText();
    }

    private void RestoreOriginalOrder()
    {
        VideoInfo? currentVideo = GetCurrentVideo();
        _playQueue = new List<VideoInfo>(_playlistVideos);

        if (currentVideo != null)
        {
            _currentPlaylistIndex = _playlistVideos.IndexOf(currentVideo);
        }
    }

    private VideoInfo? GetCurrentVideo()
    {
        return _currentPlaylistIndex >= 0 && _currentPlaylistIndex < _playQueue.Count
            ? _playQueue[_currentPlaylistIndex]
            : null;
    }

#pragma warning disable CA5394 // Random is not cryptographically secure - shuffle doesn't require crypto security
    private void ShufflePlayQueue()
    {
        var currentVideo = GetCurrentVideo();
        var shuffled = _playlistVideos.ToList();

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        _playQueue = shuffled;

        if (currentVideo != null)
        {
            _currentPlaylistIndex = _playQueue.IndexOf(currentVideo);
        }
    }
#pragma warning restore CA5394

    partial void OnSliderVolumeChanged(double value) => UpdateMediaElementVolume();

    partial void OnVolumeAmplificationChanged(double value) => UpdateMediaElementVolume();

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
        catch (UnauthorizedAccessException uaEx)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading video metadata (Unauthorized Access): {uaEx.Message}");
            VideoInfo = new VideoMetadata { FileName = "Error: Access denied" };
        }
        catch (IOException ioEx)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading video metadata (IO Exception): {ioEx.Message}");
            VideoInfo = new VideoMetadata { FileName = "Error: IO error" };
        }
        finally
        {
            _metadataUpdateLock.Release();
        }
    }

    [RelayCommand]
    private void ToggleVideoInfo() => IsVideoInfoVisible = !IsVideoInfoVisible;

    [RelayCommand]
    private async Task CaptureScreenshotAsync()
    {
        if (MediaElement == null)
        {
            await _dialogService.ShowAlertAsync(
                _resourcesProvider["Player_Error_Title"],
                _resourcesProvider["Player_Error_Playback"],
                _resourcesProvider["Button_OK"]);
            return;
        }

        try
        {
            IsBusy = true;
            await _screenshotService.CaptureScreenshotAsync(MediaElement);

            await _dialogService.ShowAlertAsync(
                _resourcesProvider["Player_Screenshot_Success"],
                _resourcesProvider["Player_Screenshot_Success"],
                _resourcesProvider["Button_OK"]);
        }
        catch (UnauthorizedAccessException authEx)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot permission error: {authEx.Message}");
            await _dialogService.ShowAlertAsync(
                _resourcesProvider["Player_Error_Title"],
                _resourcesProvider["Player_Permission_Error"],
                _resourcesProvider["Button_OK"]);
        }
        catch (IOException ioEx)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot IO error: {ioEx.Message}");
            await _dialogService.ShowAlertAsync(
                _resourcesProvider["Player_Error_Title"],
                _resourcesProvider["Player_Screenshot_Error"],
                _resourcesProvider["Button_OK"]);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot error: {ex.Message}");
            await _dialogService.ShowAlertAsync(
                _resourcesProvider["Player_Error_Title"],
                _resourcesProvider["Player_Screenshot_Error"],
                _resourcesProvider["Button_OK"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnFilePathChanged(string value)
    {
        _ = OnFilePathChangedAsync(value);
    }

    private async Task OnFilePathChangedAsync(string value)
    {
        try
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
                ResetVideoState();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnFilePathChangedAsync: {ex.Message}");
        }
    }

    private void ResetVideoState()
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
        catch (FileNotFoundException fnfEx)
        {
            System.Diagnostics.Debug.WriteLine($"Subtitle file not found: {fnfEx.Message}");
            HasSubtitles = false;
            _subtitles.Clear();
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
            if (!string.IsNullOrEmpty(CurrentSubtitleText))
                CurrentSubtitleText = string.Empty;
            return;
        }

        var activeSubtitle = FindActiveSubtitle(position);
        string newText = activeSubtitle?.Text ?? string.Empty;

        if (CurrentSubtitleText != newText)
        {
            CurrentSubtitleText = newText;
        }
    }

    private SubtitleItem? FindActiveSubtitle(TimeSpan position)
    {
        int left = 0;
        int right = _subtitles.Count - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            var subtitle = _subtitles[mid];

            if (subtitle.IsActiveAt(position))
            {
                return subtitle;
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

        return null;
    }

    [RelayCommand]
    private void ToggleSubtitles()
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
    private void ToggleMute()
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
    private async Task ToggleFavoriteAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        try
        {
            var video = new VideoInfo
            {
                FilePath = FilePath,
                Title = Path.GetFileNameWithoutExtension(FilePath),
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
            await _dialogService.ShowAlertAsync(
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
        HandleScreenWakeState(value);
        SavePositionOnPauseOrStop(value);
    }

    private void HandleScreenWakeState(MediaElementState state)
    {
        if (state == MediaElementState.Playing)
        {
            _screenWakeService.KeepScreenOn();
            System.Diagnostics.Debug.WriteLine("Media playing: Keeping screen on");
        }
        else if (state is MediaElementState.Stopped or MediaElementState.Paused
                 or MediaElementState.Failed or MediaElementState.None)
        {
            _screenWakeService.AllowScreenSleep();
            System.Diagnostics.Debug.WriteLine("Media not playing: Allowing screen to sleep");
        }
    }

    private void SavePositionOnPauseOrStop(MediaElementState state)
    {
        if ((state == MediaElementState.Paused || state == MediaElementState.Stopped) &&
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
            HandlePlaylistMediaEnded();
        }
        else
        {
            ClearSavedPosition();
        }
    }

    private void HandlePlaylistMediaEnded()
    {
        switch (RepeatMode)
        {
            case RepeatMode.One:
                SeekRequested?.Invoke(this, new SeekEventArgs(TimeSpan.Zero));
                PlayRequested?.Invoke(this, EventArgs.Empty);
                break;

            case RepeatMode.All:
                _currentPlaylistIndex = _currentPlaylistIndex < _playQueue.Count - 1
                    ? _currentPlaylistIndex + 1
                    : 0;
                PlayNextInQueue();
                break;

            case RepeatMode.None:
            default:
                if (_currentPlaylistIndex < _playQueue.Count - 1)
                {
                    _currentPlaylistIndex++;
                    PlayNextInQueue();
                }

                break;
        }
    }

    private void PlayNextInQueue()
    {
        var nextVideo = _playQueue[_currentPlaylistIndex];
        FilePath = nextVideo.FilePath;
        CheckForSavedPosition();
        UpdateProgressText();
        UpdateNavigationButtons();

        _ = Task.Delay(100).ContinueWith(_ =>
        {
            PlayRequested?.Invoke(this, EventArgs.Empty);
        }, TaskScheduler.Default);
    }

    public async Task<bool> ShouldResumeAsync()
    {
        if (!ShouldResumePlayback || ResumePosition <= TimeSpan.Zero)
            return false;

        var formattedTime = ResumePosition.ToString(@"hh\:mm\:ss");
        var message = string.Format(
            _resourcesProvider["Player_ResumeDialog_Message"],
            formattedTime);

        return await _dialogService.ShowConfirmationAsync(
            _resourcesProvider["Player_ResumeDialog_Title"],
            message,
            _resourcesProvider["Player_ResumeDialog_Resume"],
            _resourcesProvider["Player_ResumeDialog_StartOver"]);
    }

    public async Task HandleResumeDecisionAsync()
    {
        if (!ShouldResumePlayback || ResumePosition <= TimeSpan.Zero)
        {
            PlayRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        bool resume = await ShouldResumeAsync();

        if (resume)
        {
            ResumePlaybackRequested?.Invoke(this, new ResumePlaybackEventArgs(ResumePosition));
        }
        else
        {
            ClearSavedPosition();
            PlayRequested?.Invoke(this, EventArgs.Empty);
        }

        ShouldResumePlayback = false;
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
    private async Task GoBackAsync()
    {
        await _dialogService.NavigateBackAsync();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        if (_resourcesProvider is ObservableObject observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
        }

        _initializationCts?.Cancel();
        _initializationCts?.Dispose();

        Cleanup();
        _metadataUpdateLock.Dispose();
        _playlistLock.Dispose();

        _isDisposed = true;
    }
}
