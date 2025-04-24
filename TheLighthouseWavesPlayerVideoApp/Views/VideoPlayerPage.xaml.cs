using CommunityToolkit.Maui.Core.Primitives;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
using System.Timers;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoPlayerPage : ContentPage
{
    private readonly VideoPlayerViewModel _viewModel;
    private bool _isSeekingFromResume = false;
    private DateTime _lastSubtitleUpdate = DateTime.MinValue;
    private bool _metadataSuccessfullyUpdated = false;
    private System.Timers.Timer? _metadataRetryTimer;

    public VideoPlayerPage(VideoPlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        SizeChanged += OnPageSizeChanged;
    }

    private void OnPageSizeChanged(object sender, EventArgs e)
    {
        UpdateOrientationState();
    }

    private void UpdateOrientationState()
    {
        string orientationState = Width > Height ? "Landscape" : "Portrait";
        VisualStateManager.GoToState(this, orientationState);
        Shell.SetNavBarIsVisible(this, orientationState == "Portrait");
    }

    void MediaElement_StateChanged(object sender, MediaStateChangedEventArgs e)
    {
        if (_viewModel == null || mediaElement == null) return;

        _viewModel.CurrentState = e.NewState;
        System.Diagnostics.Debug.WriteLine($"MediaElement State: {e.NewState}");

        if (!_metadataSuccessfullyUpdated &&
            (e.NewState == MediaElementState.Playing || e.NewState == MediaElementState.Buffering))
        {
            System.Diagnostics.Debug.WriteLine($"Attempting metadata update on state change to {e.NewState}.");
            TryUpdateMetadata();
        }


        if (!_isSeekingFromResume &&
            (e.NewState == MediaElementState.Paused || e.NewState == MediaElementState.Stopped))
        {
            if (mediaElement.Position > TimeSpan.Zero)
            {
                _viewModel.SavePosition(mediaElement.Position);
            }
        }

        if (_isSeekingFromResume && e.NewState == MediaElementState.Playing)
        {
            _isSeekingFromResume = false;
        }
    }

    private void MediaElement_PositionChanged(object sender, MediaPositionChangedEventArgs e)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastSubtitleUpdate).TotalMilliseconds > 200)
        {
            _viewModel?.UpdateSubtitles(e.Position);
            _lastSubtitleUpdate = now;
        }
    }

    private async void MediaElement_MediaOpened(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MediaElement_MediaOpened fired.");
        _metadataSuccessfullyUpdated = false;

        if (_viewModel == null || mediaElement == null)
        {
            System.Diagnostics.Debug.WriteLine("MediaElement_MediaOpened skipped: ViewModel or MediaElement is null.");
            return;
        }

        _viewModel.MediaElement = mediaElement;

        TryUpdateMetadata();

        if (!_metadataSuccessfullyUpdated)
        {
            SetupMetadataRetryTimer();
        }

        if (_viewModel.ShouldResumePlayback && _viewModel.ResumePosition > TimeSpan.Zero)
        {
            StopAndDisposeRetryTimer();

            bool resume = await DisplayAlert("Resume Playback?",
                $"Resume from {_viewModel.ResumePosition:hh\\:mm\\:ss}?",
                "Resume", "Start Over");

            if (resume)
            {
                _isSeekingFromResume = true;
                mediaElement.SeekTo(_viewModel.ResumePosition);
                System.Diagnostics.Debug.WriteLine($"Seeking to {_viewModel.ResumePosition}");

                // mediaElement.Play();
            }
            else
            {
                _viewModel.ClearSavedPosition();
                mediaElement.Play();
                if (!_metadataSuccessfullyUpdated) SetupMetadataRetryTimer();
            }

            _viewModel.ShouldResumePlayback = false;
        }
        else
        {
            mediaElement.Play();
        }
    }

    private void TryUpdateMetadata()
    {
        if (_metadataSuccessfullyUpdated || _viewModel == null || mediaElement == null) return;

        var currentWidth = mediaElement.Width;
        var currentHeight = mediaElement.Height;
        var currentDuration = mediaElement.Duration;

        System.Diagnostics.Debug.WriteLine(
            $"TryUpdateMetadata - Path: {_viewModel.FilePath}, W: {currentWidth}, H: {currentHeight}, D: {currentDuration}");

        if (!string.IsNullOrEmpty(_viewModel.FilePath) && currentWidth > 0 && currentHeight > 0 &&
            currentDuration > TimeSpan.Zero)
        {
            _viewModel.UpdateVideoMetadata(currentWidth, currentHeight, currentDuration);
            _metadataSuccessfullyUpdated = true;

            StopAndDisposeRetryTimer();
            System.Diagnostics.Debug.WriteLine("Metadata updated successfully. Retry timer stopped.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Metadata update skipped: Invalid data detected in TryUpdateMetadata.");
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
            TryUpdateMetadata();
            if (!_metadataSuccessfullyUpdated)
            {
                System.Diagnostics.Debug.WriteLine("Metadata still not updated after retry.");
            }
        });
    }

    private void StopAndDisposeRetryTimer()
    {
        _metadataRetryTimer?.Stop();
        _metadataRetryTimer?.Dispose();
        _metadataRetryTimer = null;
    }


    private void MediaElement_MediaEnded(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MediaElement_MediaEnded fired.");
        _viewModel?.ClearSavedPosition();
        _metadataSuccessfullyUpdated = false;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        StopAndDisposeRetryTimer();

        var currentPosition = mediaElement?.Position ?? TimeSpan.Zero;
        _viewModel?.OnNavigatedFrom(currentPosition);

        if (mediaElement != null && (mediaElement.CurrentState == MediaElementState.Playing ||
                                     mediaElement.CurrentState == MediaElementState.Paused))
        {
            mediaElement.Stop();
        }

        SizeChanged -= OnPageSizeChanged;
        if (mediaElement != null)
        {
            mediaElement.StateChanged -= MediaElement_StateChanged;
            mediaElement.MediaOpened -= MediaElement_MediaOpened;
            mediaElement.MediaEnded -= MediaElement_MediaEnded;
            mediaElement.PositionChanged -= MediaElement_PositionChanged;
        }

        base.OnNavigatedFrom(args);
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnNavigatedFrom finished.");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _metadataSuccessfullyUpdated = false;

        SizeChanged -= OnPageSizeChanged;
        SizeChanged += OnPageSizeChanged;
        if (mediaElement != null)
        {
            mediaElement.StateChanged -= MediaElement_StateChanged;
            mediaElement.MediaOpened -= MediaElement_MediaOpened;
            mediaElement.MediaEnded -= MediaElement_MediaEnded;
            mediaElement.PositionChanged -= MediaElement_PositionChanged;

            mediaElement.StateChanged += MediaElement_StateChanged;
            mediaElement.MediaOpened += MediaElement_MediaOpened;
            mediaElement.MediaEnded += MediaElement_MediaEnded;
            mediaElement.PositionChanged += MediaElement_PositionChanged;
        }

        if (_viewModel != null)
        {
            await _viewModel.CheckFavoriteStatusAsync();
        }

        UpdateOrientationState();
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnAppearing finished.");
    }
}