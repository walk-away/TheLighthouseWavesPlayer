using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }
}
