using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class PlaylistsPage : ContentPage
{
    private readonly PlaylistsViewModel _viewModel;

    public PlaylistsPage(PlaylistsViewModel viewModel)
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}
