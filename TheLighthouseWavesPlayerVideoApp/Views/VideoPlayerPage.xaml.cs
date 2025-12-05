using CommunityToolkit.Maui.Core.Primitives;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
using System.Timers;
using CommunityToolkit.Maui.Views;
using TheLighthouseWavesPlayerVideoApp.Localization;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoPlayerPage : IDisposable
{
    private readonly VideoPlayerViewModel _viewModel;
    private bool _isSeekingFromResume;
    private DateTime _lastSubtitleUpdate = DateTime.MinValue;
    private bool _metadataSuccessfullyUpdated;
    private bool _isPageActive;
    private System.Timers.Timer? _metadataRetryTimer;
    private bool _isDisposed;
    private readonly object _eventLock = new();
    private bool _eventsSubscribed;
    private bool _resumeDialogShown;

    public VideoPlayerPage(VideoPlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    private void SubscribeToEvents()
    {
        lock (_eventLock)
        {
            if (_eventsSubscribed) return;

            UnsubscribeFromEventsInternal();

            SizeChanged += OnPageSizeChanged;

            if (mediaElement != null)
            {
                mediaElement.StateChanged += MediaElement_StateChanged;
                mediaElement.MediaOpened += MediaElement_MediaOpened;
                mediaElement.MediaEnded += MediaElement_MediaEnded;
                mediaElement.PositionChanged += MediaElement_PositionChanged;
                mediaElement.MediaFailed += MediaElement_MediaFailed;
            }

            _viewModel.PlayRequested += OnPlayRequested;
            _viewModel.SeekRequested += OnSeekRequested;
            _viewModel.ResumePlaybackRequested += OnResumePlaybackRequested;

            _eventsSubscribed = true;
            System.Diagnostics.Debug.WriteLine("Events subscribed");
        }
    }

    private void OnPlayRequested(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            mediaElement?.Play();
        });
    }

    private void OnResumePlaybackRequested(object? sender, ResumePlaybackEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isSeekingFromResume = true;
            mediaElement?.SeekTo(e.Position);
            mediaElement?.Play();
        });
    }

    private void OnSeekRequested(object? sender, SeekEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            mediaElement?.SeekTo(e.Position);
        });
    }

    private void UnsubscribeFromEvents()
    {
        lock (_eventLock)
        {
            UnsubscribeFromEventsInternal();
        }
    }

    private void UnsubscribeFromEventsInternal()
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

        _viewModel.PlayRequested -= OnPlayRequested;
        _viewModel.SeekRequested -= OnSeekRequested;
        _viewModel.ResumePlaybackRequested -= OnResumePlaybackRequested;

        _eventsSubscribed = false;
        System.Diagnostics.Debug.WriteLine("Events unsubscribed");
    }

    private void MediaElement_MediaFailed(object? sender, MediaFailedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorMessage}");
        _ = HandleMediaFailedAsync(e.ErrorMessage);
    }

    private async Task HandleMediaFailedAsync(string errorMessage)
    {
        try
        {
            var resources = LocalizedResourcesProvider.Instance;
            await Shell.Current.DisplayAlert(
                resources["Player_Error_Title"],
                $"{resources["Player_Error_Playback"]}\n{errorMessage}",
                resources["Button_OK"]);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error displaying failure dialog: {ex.Message}");
        }
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
                e.NewState is MediaElementState.Playing or MediaElementState.Buffering)
            {
                TryUpdateMetadata();
            }

            if (!_isSeekingFromResume &&
                e.NewState is MediaElementState.Paused or MediaElementState.Stopped &&
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
            await HandleMediaOpenedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MediaElement_MediaOpened: {ex}");
            await HandleMediaOpenedErrorAsync();
        }
    }

    private async Task HandleMediaOpenedAsync()
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
            SetupMetadataRetryTimer();

        if (_viewModel.ShouldResumePlayback && !_resumeDialogShown)
        {
            _resumeDialogShown = true;
            await _viewModel.HandleResumeDecisionAsync();
        }
/*
        if (_viewModel.ShouldResumePlayback &&
            _viewModel.ResumePosition > TimeSpan.Zero &&
            !_resumeDialogShown)
        {
            await HandleResumePlaybackAsync();
        }
*/
        else if (!_viewModel.ShouldResumePlayback &&
                 (_viewModel.IsPlaylistMode || mediaElement.ShouldAutoPlay))
        {
            mediaElement.Play();
        }
    }

    private async Task HandleResumePlaybackAsync()
    {
        _resumeDialogShown = true;

        bool resume = await _viewModel.ShouldResumeAsync();

        if (resume)
        {
            _isSeekingFromResume = true;
            mediaElement?.SeekTo(_viewModel.ResumePosition);
            mediaElement?.Play();
        }
        else
        {
            _viewModel.ClearSavedPosition();
            mediaElement?.Play();
        }

        _viewModel.ShouldResumePlayback = false;
    }

    private async Task HandleMediaOpenedErrorAsync()
    {
        var resources = LocalizedResourcesProvider.Instance;
        await Shell.Current.DisplayAlert(
            resources["Player_Error_Title"],
            resources["Player_Error_Playback"],
            resources["Button_OK"]);
    }

    private void MediaElement_MediaEnded(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MediaElement_MediaEnded fired.");
        _resumeDialogShown = false;
        _viewModel?.OnMediaEnded();
        _metadataSuccessfullyUpdated = false;
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

            if (!string.IsNullOrEmpty(_viewModel.FilePath) &&
                currentWidth > 0 && currentHeight > 0 &&
                currentDuration > TimeSpan.Zero)
            {
                _ = UpdateMetadataAsync(currentWidth, currentHeight, currentDuration);
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

    private async Task UpdateMetadataAsync(double width, double height, TimeSpan duration)
    {
        await _viewModel.UpdateVideoMetadataAsync(width, height, duration);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _metadataSuccessfullyUpdated = true;
            StopAndDisposeRetryTimer();
            System.Diagnostics.Debug.WriteLine("Metadata updated successfully. Retry timer stopped.");
        });
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

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnNavigatedFrom started.");
        _isPageActive = false;
        _resumeDialogShown = false;
        StopAndDisposeRetryTimer();

        TimeSpan currentPosition = TimeSpan.Zero;
        if (mediaElement != null &&
            mediaElement.CurrentState is MediaElementState.Playing or MediaElementState.Paused)
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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = OnAppearingAsync();
    }

    private async Task OnAppearingAsync()
    {
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
                $"OnAppearing: FilePath={_viewModel.FilePath}, ShouldResume={_viewModel.ShouldResumePlayback}, " +
                $"ResumePosition={_viewModel.ResumePosition}, LastKnownPosition={_viewModel.LastKnownPosition}");

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

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            UnsubscribeFromEvents();
            StopAndDisposeRetryTimer();
            _viewModel?.Dispose();
        }

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
