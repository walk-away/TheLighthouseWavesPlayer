using CommunityToolkit.Maui.Core.Primitives;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
using Microsoft.Maui.Storage;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoPlayerPage : ContentPage
{
    private readonly VideoPlayerViewModel _viewModel;
    private bool _isSeekingFromResume = false;
    private DateTime _lastSubtitleUpdate = DateTime.MinValue;

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
        if (_viewModel == null) return;

        _viewModel.CurrentState = e.NewState;
        System.Diagnostics.Debug.WriteLine($"MediaElement State: {e.NewState}");

        if (!_isSeekingFromResume &&
            (e.NewState == MediaElementState.Paused || e.NewState == MediaElementState.Stopped))
        {
            _viewModel.SavePosition(mediaElement.Position);
        }

        if (_isSeekingFromResume && e.NewState == MediaElementState.Playing)
        {
            _isSeekingFromResume = false;
        }
    }

    private void MediaElement_PositionChanged(object sender, MediaPositionChangedEventArgs e)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastSubtitleUpdate).TotalMilliseconds > 100)
        {
            _viewModel?.UpdateSubtitles(e.Position);
            _lastSubtitleUpdate = now;
        }
    }

    private async void MediaElement_MediaOpened(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MediaElement_MediaOpened fired.");
        if (_viewModel != null && _viewModel.ShouldResumePlayback && _viewModel.ResumePosition > TimeSpan.Zero)
        {
            bool resume = await DisplayAlert("Resume Playback?",
                $"Resume from {_viewModel.ResumePosition:hh\\:mm\\:ss}?",
                "Resume", "Start Over");

            if (resume)
            {
                _isSeekingFromResume = true;
                mediaElement.SeekTo(_viewModel.ResumePosition);
                System.Diagnostics.Debug.WriteLine($"Seeking to {_viewModel.ResumePosition}");

                mediaElement.Play();
            }
            else
            {
                _viewModel.ClearSavedPosition();
            }

            _viewModel.ShouldResumePlayback = false;
        }
    }

    private void MediaElement_MediaEnded(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MediaElement_MediaEnded fired.");
        _viewModel?.ClearSavedPosition();
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        var currentPosition = mediaElement.Position;
        _viewModel?.OnNavigatedFrom(currentPosition);

        if (mediaElement.CurrentState == MediaElementState.Playing ||
            mediaElement.CurrentState == MediaElementState.Paused)
        {
            mediaElement.Stop();
        }

        SizeChanged -= OnPageSizeChanged;
        mediaElement.StateChanged -= MediaElement_StateChanged;
        mediaElement.MediaOpened -= MediaElement_MediaOpened;
        mediaElement.MediaEnded -= MediaElement_MediaEnded;
        mediaElement.PositionChanged -= MediaElement_PositionChanged;

        base.OnNavigatedFrom(args);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        SizeChanged -= OnPageSizeChanged;
        SizeChanged += OnPageSizeChanged;
        mediaElement.StateChanged -= MediaElement_StateChanged;
        mediaElement.StateChanged += MediaElement_StateChanged;
        mediaElement.MediaOpened -= MediaElement_MediaOpened;
        mediaElement.MediaOpened += MediaElement_MediaOpened;
        mediaElement.MediaEnded -= MediaElement_MediaEnded;
        mediaElement.MediaEnded += MediaElement_MediaEnded;
        mediaElement.PositionChanged -= MediaElement_PositionChanged;
        mediaElement.PositionChanged += MediaElement_PositionChanged;

        if (_viewModel != null)
        {
            await _viewModel.CheckFavoriteStatusAsync();
        }

        UpdateOrientationState();
    }
}