using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class FavoritesPage
{
    private readonly FavoritesViewModel _viewModel;

    public FavoritesPage(FavoritesViewModel viewModel)
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
                System.Diagnostics.Debug.WriteLine($"Error in FavoritesPage.OnAppearing: {ex.Message}");
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}
