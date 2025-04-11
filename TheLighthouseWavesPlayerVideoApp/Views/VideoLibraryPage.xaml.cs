using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class VideoLibraryPage : ContentPage
{
    private VideoLibraryViewModel _viewModel;
    
    public VideoLibraryPage(VideoLibraryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        

        if (_viewModel.Videos.Count == 0)
        {
            await _viewModel.LoadVideos();
        }
    }
}