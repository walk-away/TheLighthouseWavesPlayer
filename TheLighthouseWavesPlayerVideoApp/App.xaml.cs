using TheLighthouseWavesPlayerVideoApp.Converters;
using TheLighthouseWavesPlayerVideoApp.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var boolToColorConverter = Resources["BoolToColorConverter"] as BoolToColorConverter;
        if (boolToColorConverter != null)
        {
            boolToColorConverter.TrueColor = (Color)Resources["Primary"];
            boolToColorConverter.FalseColor = Colors.Transparent;
        }

        var themeService = IPlatformApplication.Current.Services.GetService<IThemeService>();
        themeService?.ApplyTheme();

        MainPage = new AppShell();
    }
}