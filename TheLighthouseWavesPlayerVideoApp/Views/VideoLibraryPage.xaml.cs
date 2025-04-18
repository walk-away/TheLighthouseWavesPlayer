using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoLibraryPage : ContentPage
{
    private readonly VideoLibraryViewModel _viewModel;

    public VideoLibraryPage(VideoLibraryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            await _viewModel.OnAppearing();
        }
    }
}