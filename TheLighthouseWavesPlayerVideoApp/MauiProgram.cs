using System.Resources;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization;
using TheLighthouseWavesPlayerVideoApp.Localization.Helpers;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Services;
using TheLighthouseWavesPlayerVideoApp.ViewModels;
using TheLighthouseWavesPlayerVideoApp.Views;

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

        builder.Services.AddSingleton<IVideoDatabase, VideoDatabase>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<IScreenWakeService, ScreenWakeService>();

        var resourceManager = new ResourceManager("TheLighthouseWavesPlayerVideoApp.Resources.Languages.AppResources",
            typeof(MauiProgram).Assembly);
        builder.Services.AddSingleton<ILocalizedResourcesProvider>(new LocalizedResourcesProvider(resourceManager));
        builder.Services.AddSingleton<ILocalizationManager, LocalizationManager>();

        builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
        builder.Services.AddSingleton<IPlaylistService, PlaylistService>();
        builder.Services.AddSingleton<IVideoDiscoveryService, VideoDiscoveryService>();
        builder.Services.AddSingleton<ISubtitleService, SubtitleService>();
        builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();

        builder.Services.AddTransient<VideoPlayerViewModel>();
        builder.Services.AddSingleton<VideoLibraryViewModel>();
        builder.Services.AddSingleton<FavoritesViewModel>();
        builder.Services.AddSingleton<PlaylistsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        builder.Services.AddTransient<VideoPlayerPage>();
        builder.Services.AddSingleton<VideoLibraryPage>();
        builder.Services.AddSingleton<PlaylistsPage>();
        builder.Services.AddSingleton<FavoritesPage>();
        builder.Services.AddSingleton<SettingsPage>();

        return builder.Build();
    }
}
