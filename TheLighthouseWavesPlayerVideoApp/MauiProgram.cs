using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Services;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
using TheLighthouseWavesPlayerVideoApp.Views;
#if ANDROID

#endif

namespace TheLighthouseWavesPlayerVideoApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif


        builder.Services.AddSingleton<VideoDatabase>();
        
        builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
        
#if ANDROID
        builder.Services.AddSingleton<IVideoDiscoveryService, VideoDiscoveryService>();
#else
        // Register a dummy or throw exception if building for other platforms
        // builder.Services.AddSingleton<IVideoDiscoveryService, DummyVideoDiscoveryService>();
#endif
        
        builder.Services.AddSingleton<VideoLibraryViewModel>();
        builder.Services.AddSingleton<FavoritesViewModel>();
        builder.Services.AddTransient<VideoPlayerViewModel>();
        
        builder.Services.AddSingleton<VideoLibraryPage>();
        builder.Services.AddSingleton<FavoritesPage>();
        builder.Services.AddTransient<VideoPlayerPage>();


        return builder.Build();
    }
}