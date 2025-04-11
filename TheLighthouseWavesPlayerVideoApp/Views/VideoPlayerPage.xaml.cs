using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoPlayerPage : ContentPage
{
    private VideoPlayerViewModel _viewModel;
    
    public VideoPlayerPage(VideoPlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void OnMediaOpened(object sender, EventArgs e)
    {
        if (_viewModel != null && mediaElement != null)
        {
            // Get duration from media element
            _viewModel.Duration = mediaElement.Duration.TotalSeconds;
            
            // If there's a saved position, seek to it
            if (_viewModel.Position > 0)
            {
                mediaElement.SeekTo(TimeSpan.FromSeconds(_viewModel.Position));
            }
            
            // Start playing
            if (_viewModel.IsPlaying)
            {
                mediaElement.Play();
            }
        }
    }

    private void OnMediaEnded(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            // Reset position to beginning
            _viewModel.Position = 0;
            _viewModel.IsPlaying = false;
        }
    }

    private void OnRewindClicked(object sender, EventArgs e)
    {
        if (mediaElement != null)
        {
            // Rewind 10 seconds
            double newPosition = Math.Max(0, _viewModel.Position - 10);
            _viewModel.Position = newPosition;
            mediaElement.SeekTo(TimeSpan.FromSeconds(newPosition));
        }
    }

    private void OnForwardClicked(object sender, EventArgs e)
    {
        if (mediaElement != null && _viewModel != null)
        {
            // Forward 10 seconds
            double newPosition = Math.Min(_viewModel.Duration, _viewModel.Position + 10);
            _viewModel.Position = newPosition;
            mediaElement.SeekTo(TimeSpan.FromSeconds(newPosition));
        }
    }

    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (mediaElement != null && Math.Abs(e.NewValue - e.OldValue) > 1)
        {
            // Only seek if the change is significant (to avoid constant seeking during normal playback)
            mediaElement.SeekTo(TimeSpan.FromSeconds(e.NewValue));
        }
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
    
        // Display diagnostic information
        if (BindingContext is VideoPlayerViewModel vm)
        {
            if (vm.Video == null)
            {
                DisplayAlert("Debug", "Video is null in OnAppearing", "OK");
            
                // Try manually setting the VideoId if navigation parameter failed
                if (vm.VideoId <= 0)
                {
                    // This is just for testing - remove in production
                    var testId = 1; // Use a known valid ID
                    DisplayAlert("Debug", $"Trying to manually load video with ID: {testId}", "OK");
                    vm.VideoId = testId;
                }
            }
            else
            {
                DisplayAlert("Debug", $"Playing video: {vm.Video.Title}", "OK");
            }
        }
        else
        {
            DisplayAlert("Debug", "ViewModel not set", "OK");
        }
    }
}