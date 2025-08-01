using CommunityToolkit.Maui.Core.Primitives;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
using System.Timers;
using CommunityToolkit.Maui.Views;
using TheLighthouseWavesPlayerVideoApp.Localization;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoPlayerPage : ContentPage, IDisposable
{
    private readonly VideoPlayerViewModel _viewModel;
    private bool _isSeekingFromResume = false;
    private DateTime _lastSubtitleUpdate = DateTime.MinValue;
    private bool _metadataSuccessfullyUpdated = false;
    private bool _isPageActive = false;
    private System.Timers.Timer? _metadataRetryTimer;
    private bool _isDisposed = false;
    private readonly object _eventLock = new object();
    private bool _eventsSubscribed = false;
    private bool _resumeDialogShown = false;

    public VideoPlayerPage(VideoPlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    private void SubscribeToEvents()
    {
        lock (_eventLock)
        {
            if (!_eventsSubscribed)
            {
                UnsubscribeFromEvents();

                SizeChanged += OnPageSizeChanged;

                if (mediaElement != null)
                {
                    mediaElement.StateChanged += MediaElement_StateChanged;
                    mediaElement.MediaOpened += MediaElement_MediaOpened;
                    mediaElement.MediaEnded += MediaElement_MediaEnded;
                    mediaElement.PositionChanged += MediaElement_PositionChanged;
                    mediaElement.MediaFailed += MediaElement_MediaFailed;
                }

                _eventsSubscribed = true;
                System.Diagnostics.Debug.WriteLine("Events subscribed");
            }
        }
    }

    private void UnsubscribeFromEvents()
    {
        lock (_eventLock)
        {
            SizeChanged -= OnPageSizeChanged;

            if (mediaElement != null)
            {
                mediaElement.StateChanged -= MediaElement_StateChanged;
                mediaElement.MediaOpened -= MediaElement_MediaOpened;
                mediaElement.MediaEnded -= MediaElement_MediaEnded;
                mediaElement.PositionChanged -= MediaElement_PositionChanged;
                mediaElement.MediaFailed -= MediaElement_MediaFailed;
            }

            _eventsSubscribed = false;
            System.Diagnostics.Debug.WriteLine("Events unsubscribed");
        }
    }

    private void MediaElement_MediaFailed(object? sender, MediaFailedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorMessage}");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var resources = LocalizedResourcesProvider.Instance;
                await Shell.Current.DisplayAlert(
                    resources["Player_Error_Title"],
                    $"{resources["Player_Error_Playback"]}\n{e.ErrorMessage}",
                    resources["Button_OK"]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying failure dialog: {ex.Message}");
            }
        });
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        try
        {
            UpdateOrientationState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating orientation state: {ex.Message}");
        }
    }

    private void UpdateOrientationState()
    {
        string orientationState = Width > Height ? "Landscape" : "Portrait";
        bool isLandscape = orientationState == "Landscape";

        try
        {
            VisualStateManager.GoToState(this, orientationState);
            Shell.SetNavBarIsVisible(this, !isLandscape);

            if (BackButtonFrame != null)
            {
                BackButtonFrame.IsVisible = isLandscape;
            }

            _viewModel.IsLandscape = isLandscape;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update visual state: {ex.Message}");
        }
    }

    private void MediaElement_StateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        if (mediaElement == null || _viewModel == null) return;

        _viewModel.CurrentState = e.NewState;
        System.Diagnostics.Debug.WriteLine($"MediaElement State: {e.NewState}");

        try
        {
            if (!_metadataSuccessfullyUpdated &&
                (e.NewState == MediaElementState.Playing || e.NewState == MediaElementState.Buffering))
            {
                TryUpdateMetadata();
            }

            if (!_isSeekingFromResume &&
                (e.NewState == MediaElementState.Paused || e.NewState == MediaElementState.Stopped) &&
                mediaElement.Position > TimeSpan.Zero)
            {
                _viewModel.SavePosition(mediaElement.Position);
            }

            if (_isSeekingFromResume && e.NewState == MediaElementState.Playing)
            {
                _isSeekingFromResume = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MediaElement_StateChanged: {ex.Message}");
        }
    }

    private void MediaElement_PositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        try
        {
            var now = DateTime.UtcNow;
            if ((now - _lastSubtitleUpdate).TotalMilliseconds > 200)
            {
                _viewModel.UpdateSubtitles(e.Position);
                _lastSubtitleUpdate = now;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating subtitles: {ex.Message}");
        }
    }

    private async void MediaElement_MediaOpened(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MediaElement_MediaOpened fired.");

            if (!_isPageActive || mediaElement == null || _viewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("MediaElement_MediaOpened skipped: Preconditions not met.");
                return;
            }

            _viewModel.MediaElement = mediaElement;
            _viewModel.UpdateMediaElementVolume();

            TryUpdateMetadata();
            if (!_metadataSuccessfullyUpdated)
            {
                SetupMetadataRetryTimer();
            }

            if (_viewModel.IsPlaylistMode)
            {
                _resumeDialogShown = false;
            }

            if (_viewModel.ShouldResumePlayback && _viewModel.ResumePosition > TimeSpan.Zero && !_resumeDialogShown)
            {
                _resumeDialogShown = true;

                var resources = LocalizedResourcesProvider.Instance;
                var title = resources["Player_ResumeDialog_Title"];
                var messageFormat = resources["Player_ResumeDialog_Message"];
                var resumeButton = resources["Player_ResumeDialog_Resume"];
                var startOverButton = resources["Player_ResumeDialog_StartOver"];

                var formattedTime = _viewModel.ResumePosition.ToString(@"hh\:mm\:ss");
                var message = string.Format(messageFormat, formattedTime);

                bool resume = await DisplayAlert(title, message, resumeButton, startOverButton);

                if (resume)
                {
                    _isSeekingFromResume = true;
                    mediaElement.SeekTo(_viewModel.ResumePosition);
                    mediaElement.Play();
                }
                else
                {
                    _viewModel.ClearSavedPosition();
                    mediaElement.Play();
                }

                _viewModel.ShouldResumePlayback = false;
            }
            else if (_viewModel.IsPlaylistMode || mediaElement.ShouldAutoPlay)
            {
                mediaElement.Play();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MediaElement_MediaOpened: {ex}");
            var resources = LocalizedResourcesProvider.Instance;
            await Shell.Current.DisplayAlert(
                resources["Player_Error_Title"],
                resources["Player_Error_Playback"],
                resources["Button_OK"]);
        }
    }

    private void TryUpdateMetadata()
    {
        if (_metadataSuccessfullyUpdated || _viewModel == null || mediaElement == null) return;

        try
        {
            var currentWidth = mediaElement.Width;
            var currentHeight = mediaElement.Height;
            var currentDuration = mediaElement.Duration;

            System.Diagnostics.Debug.WriteLine(
                $"TryUpdateMetadata - Path: {_viewModel.FilePath}, W: {currentWidth}, H: {currentHeight}, D: {currentDuration}");

            if (!string.IsNullOrEmpty(_viewModel.FilePath) && currentWidth > 0 && currentHeight > 0 &&
                currentDuration > TimeSpan.Zero)
            {
                Task.Run(async () =>
                {
                    await _viewModel.UpdateVideoMetadataAsync(currentWidth, currentHeight, currentDuration);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _metadataSuccessfullyUpdated = true;
                        StopAndDisposeRetryTimer();
                        System.Diagnostics.Debug.WriteLine("Metadata updated successfully. Retry timer stopped.");
                    });
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    "Metadata update skipped: Invalid data detected in TryUpdateMetadata.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in TryUpdateMetadata: {ex.Message}");
        }
    }

    private void SetupMetadataRetryTimer()
    {
        StopAndDisposeRetryTimer();

        _metadataRetryTimer = new System.Timers.Timer(1000);
        _metadataRetryTimer.Elapsed += OnMetadataRetryTimerElapsed;
        _metadataRetryTimer.AutoReset = false;
        _metadataRetryTimer.Start();
        System.Diagnostics.Debug.WriteLine("Metadata retry timer started.");
    }

    private void OnMetadataRetryTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Metadata retry timer elapsed.");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_isDisposed)
            {
                TryUpdateMetadata();
                if (!_metadataSuccessfullyUpdated)
                {
                    System.Diagnostics.Debug.WriteLine("Metadata still not updated after retry.");
                }
            }
        });
    }

    private void StopAndDisposeRetryTimer()
    {
        if (_metadataRetryTimer != null)
        {
            _metadataRetryTimer.Elapsed -= OnMetadataRetryTimerElapsed;
            _metadataRetryTimer.Stop();
            _metadataRetryTimer.Dispose();
            _metadataRetryTimer = null;
        }
    }

    private void MediaElement_MediaEnded(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MediaElement_MediaEnded fired.");
    
        _viewModel?.OnMediaEnded();
        
        _metadataSuccessfullyUpdated = false;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnNavigatedFrom started.");
        _isPageActive = false;
        _resumeDialogShown = false;
        StopAndDisposeRetryTimer();

        TimeSpan currentPosition = TimeSpan.Zero;
        if (mediaElement != null && (mediaElement.CurrentState == MediaElementState.Playing ||
                                     mediaElement.CurrentState == MediaElementState.Paused))
        {
            currentPosition = mediaElement.Position;
            mediaElement.Stop();
            System.Diagnostics.Debug.WriteLine($"Stopped MediaElement. Current position: {currentPosition}");
        }

        _viewModel?.OnNavigatedFrom(currentPosition);

        UnsubscribeFromEvents();

        base.OnNavigatedFrom(args);
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnNavigatedFrom finished.");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnAppearing started.");
        _isPageActive = true;
        _metadataSuccessfullyUpdated = false;
        _resumeDialogShown = false;

        UnsubscribeFromEvents();
        SubscribeToEvents();

        if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.FilePath) && mediaElement != null)
        {
            _viewModel.CheckForSavedPosition();

            System.Diagnostics.Debug.WriteLine(
                $"OnAppearing: FilePath={_viewModel.FilePath}, ShouldResume={_viewModel.ShouldResumePlayback}, ResumePosition={_viewModel.ResumePosition}, LastKnownPosition={_viewModel.LastKnownPosition}");

            if (mediaElement.Source == null ||
                (mediaElement.Source is FileMediaSource fms && fms.Path != _viewModel.FilePath))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Setting MediaElement Source in OnAppearing to: {_viewModel.FilePath}");
                _viewModel.VideoSource = MediaSource.FromFile(_viewModel.FilePath);
                await Task.Delay(100);
            }

            await _viewModel.CheckFavoriteStatusAsync();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("OnAppearing: ViewModel, FilePath, or MediaElement is null/empty.");
        }

        UpdateOrientationState();
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnAppearing finished.");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _viewModel?.Cleanup();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        UnsubscribeFromEvents();
        StopAndDisposeRetryTimer();
        _viewModel?.Dispose();

        _isDisposed = true;
    }
}