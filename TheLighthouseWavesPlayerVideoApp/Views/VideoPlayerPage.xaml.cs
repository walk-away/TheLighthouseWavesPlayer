using CommunityToolkit.Maui.Core.Primitives;
using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoPlayerPage : ContentPage
{
    private readonly VideoPlayerViewModel _viewModel;

    public VideoPlayerPage(VideoPlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    void MediaElement_StateChanged(object sender, MediaStateChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CurrentState = e.NewState;
            System.Diagnostics.Debug.WriteLine($"MediaElement State: {e.NewState}");
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        _viewModel?.OnNavigatedFrom();

        if (mediaElement.CurrentState == MediaElementState.Playing ||
            mediaElement.CurrentState == MediaElementState.Paused)
        {
            mediaElement.Stop();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            await _viewModel.CheckFavoriteStatusAsync();
        }
    }
}