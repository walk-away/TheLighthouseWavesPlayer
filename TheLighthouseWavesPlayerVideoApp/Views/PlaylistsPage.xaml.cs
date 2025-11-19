using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class PlaylistsPage
{
    private readonly PlaylistsViewModel _viewModel;

    public PlaylistsPage(PlaylistsViewModel viewModel)
    {
        InitializeComponent();
        ArgumentNullException.ThrowIfNull(viewModel);
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = OnAppearingAsync();
    }

    private async Task OnAppearingAsync()
    {
        try
        {
            await _viewModel.OnAppearing();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in PlaylistsPage.OnAppearing: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}
