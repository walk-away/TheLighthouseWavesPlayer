using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoPlayerPage : ContentPage
{
    private VideoPlayerViewModel _viewModel;
    private bool _isPropertyChanging = false;
    private bool _userIsDraggingSlider = false;

    public VideoPlayerPage(VideoPlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isPropertyChanging)
            return;

        _isPropertyChanging = true;
        try
        {
            if (e.PropertyName == nameof(_viewModel.IsPlaying))
            {
                if (_viewModel.IsPlaying)
                {
                    mediaElement?.Play();
                }
                else
                {
                    mediaElement?.Pause();
                }
            }

            else if (e.PropertyName == nameof(_viewModel.Position) && !_userIsDraggingSlider)
            {
                if (mediaElement != null)
                {
                    TimeSpan currentPos = mediaElement.Position;
                    TimeSpan targetPos = TimeSpan.FromSeconds(_viewModel.Position);
                    
                    if (Math.Abs((currentPos - targetPos).TotalSeconds) > 0.5)
                    {
                        mediaElement.SeekTo(targetPos);
                    }
                }
            }
        }
        finally
        {
            _isPropertyChanging = false;
        }
    }

    private void OnMediaOpened(object sender, EventArgs e)
    {
        if (_viewModel != null && mediaElement != null)
        {
            _viewModel.Duration = mediaElement.Duration.TotalSeconds;

            if (_viewModel.Position > 0)
            {
                mediaElement.SeekTo(TimeSpan.FromSeconds(_viewModel.Position));
            }

            if (_viewModel.IsPlaying)
            {
                mediaElement.Play();
            }
            else
            {
                mediaElement.Pause();
            }
        }
    }

    private void OnMediaEnded(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Position = 0;
            _viewModel.IsPlaying = false;
        }
    }

    private void OnRewindClicked(object sender, EventArgs e)
    {
        if (mediaElement != null)
        {
            double newPosition = Math.Max(0, _viewModel.Position - 10);
            _viewModel.Position = newPosition;
            mediaElement.SeekTo(TimeSpan.FromSeconds(newPosition));
        }
    }

    private void OnForwardClicked(object sender, EventArgs e)
    {
        if (mediaElement != null && _viewModel != null)
        {
            double newPosition = Math.Min(_viewModel.Duration, _viewModel.Position + 10);
            _viewModel.Position = newPosition;
            mediaElement.SeekTo(TimeSpan.FromSeconds(newPosition));
        }
    }

    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (mediaElement == null) return;
        
        if (Math.Abs(e.NewValue - e.OldValue) < 1 && !_userIsDraggingSlider) 
            return;
        
        try
        {
            _isPropertyChanging = true;
            
            mediaElement.SeekTo(TimeSpan.FromSeconds(e.NewValue));
            _viewModel.Position = e.NewValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeking video: {ex.Message}");
        }
        finally
        {
            _isPropertyChanging = false;
        }
    }
    
    private void OnSliderDragStarted(object sender, EventArgs e)
    {
        _userIsDraggingSlider = true;
    }
    
    private void OnSliderDragCompleted(object sender, EventArgs e)
    {
        _userIsDraggingSlider = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is VideoPlayerViewModel vm)
        {
            if (vm.Video == null)
            {
                DisplayAlert("Debug", "Video is null in OnAppearing", "OK");

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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            
            if (mediaElement != null && _viewModel.Video != null)
            {
                _viewModel.SaveCurrentPosition(
                    mediaElement.Position.TotalSeconds,
                    mediaElement.Duration.TotalSeconds);
            }
        }

        mediaElement?.Pause();
    }
}