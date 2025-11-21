using System.Globalization;
using TheLighthouseWavesPlayerVideoApp.Converters;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp;

public partial class App
{
    private readonly ILocalizationManager _localizationManager;
    private readonly IPermissionService _permissionService;
    private readonly IThemeService? _themeService;

    public App(
        ILocalizationManager localizationManager,
        IThemeService themeService,
        IPermissionService permissionService)
    {
        InitializeComponent();

        _localizationManager = localizationManager;
        _permissionService = permissionService;
        _themeService = themeService;

        _localizationManager.RestorePreviousCulture(CultureInfo.GetCultureInfo("en-US"));

        ConfigureConverters();

        _themeService?.ApplyTheme();

        MainPage = new AppShell();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await CheckPermissionsAsync();
        });
    }

    private void ConfigureConverters()
    {
        try
        {
            var boolToColorConverter = Resources["BoolToColorConverter"] as BoolToColorConverter;
            if (boolToColorConverter != null)
            {
                boolToColorConverter.TrueColor = (Color)Resources["Primary"];
                boolToColorConverter.FalseColor = Colors.Transparent;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error configuring converters: {ex.Message}");
        }
    }

    private async Task CheckPermissionsAsync()
    {
        try
        {
            await Task.Delay(500);

            var hasPermission = await _permissionService.CheckAndRequestStoragePermissionAsync();

            if (!hasPermission)
            {
                System.Diagnostics.Debug.WriteLine("Storage permission was denied by user");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Storage permission granted");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking permissions: {ex.Message}");
        }
    }
}
