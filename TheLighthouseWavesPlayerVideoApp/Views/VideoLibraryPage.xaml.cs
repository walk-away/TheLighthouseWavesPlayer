using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoLibraryPage : ContentPage
{
    // Hold a reference to the ViewModel
    private readonly VideoLibraryViewModel _viewModel;

    public VideoLibraryPage(VideoLibraryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel; // Store the viewModel
    }

    // Load data when the page appears
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Call the ViewModel's OnAppearing method
        if (_viewModel != null)
        {
            await _viewModel.OnAppearing();
        }
    }
}