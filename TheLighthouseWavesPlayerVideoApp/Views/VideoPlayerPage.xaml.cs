using CommunityToolkit.Maui.Core.Primitives;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
// Required for MediaElementState

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

    // Optional: Handle state changes if needed in code-behind
    void MediaElement_StateChanged(object sender, MediaStateChangedEventArgs e)
    {
        // You could update the ViewModel's state property here if needed
        if(_viewModel != null)
        {
            _viewModel.CurrentState = e.NewState;
            System.Diagnostics.Debug.WriteLine($"MediaElement State: {e.NewState}");
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        // Call ViewModel cleanup when navigating away
        _viewModel?.OnNavigatedFrom();

        // Explicitly stop the MediaElement if it's playing/paused
        // to release resources immediately, especially on Android.
        if (mediaElement.CurrentState == MediaElementState.Playing ||
            mediaElement.CurrentState == MediaElementState.Paused)
        {
            mediaElement.Stop();
        }
    }

    // Optional: Reload favorite status when page appears, in case it was changed elsewhere
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            // Re-check favorite status when the page appears
            await _viewModel.CheckFavoriteStatusAsync();
        }
    }
}