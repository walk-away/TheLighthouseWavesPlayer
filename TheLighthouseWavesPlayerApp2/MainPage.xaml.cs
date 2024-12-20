using CommunityToolkit.Maui.Core.Primitives;
using TheLighthouseWavesPlayerApp2.ViewModels;

namespace TheLighthouseWavesPlayerApp2;

public partial class MainPage : ContentPage
{
    private readonly VideoViewModel _viewModel;

    public MainPage(VideoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        _viewModel.MediaElement = MediaElement;
    }

    
    
    
    private void OnMediaOpened(object sender, EventArgs e) => _viewModel.OnMediaOpened(sender, e);
    private void OnMediaEnded(object sender, EventArgs e) => _viewModel.OnMediaEnded(sender, e);
    /*
    private void OnMediaFailed(object sender, MediaFailedEventArgs e) => _viewModel.OnMediaFailed(sender, e);
    private void OnPositionChanged(object sender, MediaPositionChangedEventArgs e) => _viewModel.OnPositionChanged(sender, e);
    private void OnStateChanged(object sender, MediaStateChangedEventArgs e) => _viewModel.OnStateChanged(sender, e);
    private void OnSeekCompleted(object sender, EventArgs e) => _viewModel.OnSeekCompleted(sender, e);
    private void Slider_DragStarted(object sender, EventArgs e) => _viewModel.Slider_DragStarted(sender, e);
    private async void Slider_DragCompleted(object sender, EventArgs e) => await _viewModel.Slider_DragCompleted(sender, e);
    */
}