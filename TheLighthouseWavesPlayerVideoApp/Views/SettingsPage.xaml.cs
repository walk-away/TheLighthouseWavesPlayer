using TheLighthouseWavesPlayerVideoApp.ViewModels;

namespace TheLighthouseWavesPlayerVideoApp.Views;

public partial class SettingsPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ArgumentNullException.ThrowIfNull(viewModel);
        BindingContext = viewModel;
        _viewModel = viewModel;
    }
}
