using TheLighthouseWavesPlayerVideoApp.Converters;

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

        MainPage = new AppShell();
    }
}