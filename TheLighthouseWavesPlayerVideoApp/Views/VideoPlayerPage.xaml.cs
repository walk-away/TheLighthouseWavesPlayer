using CommunityToolkit.Maui.Core.Primitives;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
using System.Timers;
using CommunityToolkit.Maui.Views;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoPlayerPage : ContentPage
{
    private readonly VideoPlayerViewModel _viewModel;
    private bool _isSeekingFromResume = false;
    private DateTime _lastSubtitleUpdate = DateTime.MinValue;
    private bool _metadataSuccessfullyUpdated = false;
    private bool _isPageActive = false;
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
        try
        {
            System.Diagnostics.Debug.WriteLine("MediaElement_MediaOpened fired.");
            _metadataSuccessfullyUpdated = false;

            if (_viewModel == null || mediaElement == null || !_isPageActive)
            {
                System.Diagnostics.Debug.WriteLine(
                    "MediaElement_MediaOpened skipped: ViewModel, MediaElement is null, or page is not active.");
                return;
            }

            _viewModel.MediaElement = mediaElement;
            mediaElement.Volume = _viewModel.SliderVolume * _viewModel.VolumeAmplification;

            TryUpdateMetadata();
            if (!_metadataSuccessfullyUpdated)
            {
                SetupMetadataRetryTimer();
            }
            
            if (_viewModel.ShouldResumePlayback && _viewModel.ResumePosition > TimeSpan.Zero)
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to resume playback at: {_viewModel.ResumePosition}");
                _isSeekingFromResume = true;
                mediaElement.SeekTo(_viewModel.ResumePosition);
                _viewModel.ShouldResumePlayback = false;
                _viewModel.ResumePosition = TimeSpan.Zero;
            }
            else
            {
                 System.Diagnostics.Debug.WriteLine("Starting playback from beginning or letting AutoPlay handle it.");
                 if (mediaElement.ShouldAutoPlay && mediaElement.CurrentState != MediaElementState.Playing)
                 {
                 }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MediaElement_MediaOpened: {ex}");
            await Shell.Current.DisplayAlert("Error", "Video playback error", "OK");
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
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnNavigatedFrom started.");
        _isPageActive = false;
        StopAndDisposeRetryTimer();

        TimeSpan currentPosition = TimeSpan.Zero;
        if (mediaElement != null && (mediaElement.CurrentState == MediaElementState.Playing || mediaElement.CurrentState == MediaElementState.Paused))
        {
            currentPosition = mediaElement.Position;
            mediaElement.Stop();
            System.Diagnostics.Debug.WriteLine($"Stopped MediaElement. Current position: {currentPosition}");
        }
        
        _viewModel?.OnNavigatedFrom(currentPosition);
        
        // SizeChanged -= OnPageSizeChanged;
        // if (mediaElement != null)
        // {
        //     mediaElement.StateChanged -= MediaElement_StateChanged;
        //     mediaElement.MediaOpened -= MediaElement_MediaOpened;
        //     mediaElement.MediaEnded -= MediaElement_MediaEnded;
        //     mediaElement.PositionChanged -= MediaElement_PositionChanged;
        // }

        base.OnNavigatedFrom(args);
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnNavigatedFrom finished.");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnAppearing started.");
        _isPageActive = true;
        _metadataSuccessfullyUpdated = false;
        
        SizeChanged -= OnPageSizeChanged;
        SizeChanged += OnPageSizeChanged;

        if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.FilePath) && mediaElement != null)
        {
            _viewModel.CheckForSavedPosition();

            System.Diagnostics.Debug.WriteLine(
                $"OnAppearing: FilePath={_viewModel.FilePath}, ShouldResume={_viewModel.ShouldResumePlayback}, ResumePosition={_viewModel.ResumePosition}, LastKnownPosition={_viewModel.LastKnownPosition}");


            if (mediaElement.Source == null || (mediaElement.Source is FileMediaSource fms && fms.Path != _viewModel.FilePath))
            {
                 System.Diagnostics.Debug.WriteLine($"Setting MediaElement Source in OnAppearing to: {_viewModel.FilePath}");
                 _viewModel.VideoSource = MediaSource.FromFile(_viewModel.FilePath);
                 await Task.Delay(100);
            }
            
            mediaElement.StateChanged -= MediaElement_StateChanged;
            mediaElement.MediaOpened -= MediaElement_MediaOpened;
            mediaElement.MediaEnded -= MediaElement_MediaEnded;
            mediaElement.PositionChanged -= MediaElement_PositionChanged;

            mediaElement.StateChanged += MediaElement_StateChanged;
            mediaElement.MediaOpened += MediaElement_MediaOpened;
            mediaElement.MediaEnded += MediaElement_MediaEnded;
            mediaElement.PositionChanged += MediaElement_PositionChanged;

            await _viewModel.CheckFavoriteStatusAsync();
        }
        else
        {
             System.Diagnostics.Debug.WriteLine("OnAppearing: ViewModel, FilePath, or MediaElement is null/empty.");
        }

        UpdateOrientationState();
        System.Diagnostics.Debug.WriteLine("VideoPlayerPage.OnAppearing finished.");
    }
}