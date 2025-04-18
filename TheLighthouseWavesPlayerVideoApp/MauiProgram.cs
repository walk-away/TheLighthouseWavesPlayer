using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using TheLighthouseWavesPlayerVideoApp;
using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Services;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
using TheLighthouseWavesPlayerVideoApp.Views;
#if ANDROID

#endif

namespace VideoPlayerApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // Use Community Toolkit helpers (MVVM, Converters, etc.)
            .UseMauiCommunityToolkitMediaElement() // Use MediaElement
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // --- Dependency Injection Registration ---

        // Data Layer
        builder.Services.AddSingleton<VideoDatabase>(); // SQLite Database Context

        // Services
        builder.Services.AddSingleton<IFavoritesService, FavoritesService>();

        // Platform-specific Services
#if ANDROID
        builder.Services.AddSingleton<IVideoDiscoveryService, VideoDiscoveryService>();
#else
        // Register a dummy or throw exception if building for other platforms
        // builder.Services.AddSingleton<IVideoDiscoveryService, DummyVideoDiscoveryService>();
#endif

        // ViewModels
        builder.Services.AddSingleton<VideoLibraryViewModel>();
        builder.Services.AddSingleton<FavoritesViewModel>();
        builder.Services.AddTransient<VideoPlayerViewModel>(); // Transient: New instance each time it's requested

        // Views (Pages)
        builder.Services.AddSingleton<VideoLibraryPage>();
        builder.Services.AddSingleton<FavoritesPage>();
        builder.Services.AddTransient<VideoPlayerPage>(); // Transient: New instance each time it's navigated to


        return builder.Build();
    }
}