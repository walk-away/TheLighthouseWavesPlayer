using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class PlaylistsPage
{
    private readonly PlaylistsViewModel _viewModel;

    public PlaylistsPage(PlaylistsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Task.Run(async () =>
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.OnAppearing();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PlaylistsPage.OnAppearing: {ex.Message}");
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}
