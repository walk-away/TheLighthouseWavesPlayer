using System.Globalization;
using TheLighthouseWavesPlayerVideoApp.Converters;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp;

public partial class App : Application
{
    private readonly ILocalizationManager _localizationManager;
    
    public App(ILocalizationManager localizationManager, IThemeService themeService)
    {
        InitializeComponent();

        _localizationManager = localizationManager;
        _localizationManager.RestorePreviousCulture(CultureInfo.GetCultureInfo("en-US"));

        var boolToColorConverter = Resources["BoolToColorConverter"] as BoolToColorConverter;
        if (boolToColorConverter != null)
        {
            boolToColorConverter.TrueColor = (Color)Resources["Primary"];
            boolToColorConverter.FalseColor = Colors.Transparent;
        }

        themeService?.ApplyTheme();

        MainPage = new AppShell();
    }
}