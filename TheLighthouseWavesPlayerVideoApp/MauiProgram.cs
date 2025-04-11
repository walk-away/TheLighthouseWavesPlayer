using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TheLighthouseWavesPlayerVideoApp.Data;
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
            .RegisterServices()
            .RegisterViewModels()
            .RegisterViews()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    public static MauiAppBuilder RegisterServices(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        mauiAppBuilder.Services.AddSingleton<IPermissionService, PermissionService>();
        mauiAppBuilder.Services.AddSingleton<IMediaService, MediaService>();

        return mauiAppBuilder;
    }

    public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<VideoLibraryViewModel>();
        mauiAppBuilder.Services.AddSingleton<FavoritesViewModel>();
        mauiAppBuilder.Services.AddTransient<VideoPlayerViewModel>();

        return mauiAppBuilder;
    }

    public static MauiAppBuilder RegisterViews(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<VideoLibraryPage>();
        mauiAppBuilder.Services.AddSingleton<FavoritesPage>();
        mauiAppBuilder.Services.AddTransient<VideoPlayerPage>();

        return mauiAppBuilder;
    }
}