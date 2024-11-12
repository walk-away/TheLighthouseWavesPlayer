using Microsoft.Extensions.Logging;
using TheLighthouseWavesPlayer.Core.Interfaces.Explorer;
using TheLighthouseWavesPlayerApp.Extensions;


namespace TheLighthouseWavesPlayerApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        
        builder.Services.AddTheLighthouseWavesPlayerServices();

        builder.Services.AddViewModels();
        builder.Services.AddViews();
        
        return builder.Build();
    }
}