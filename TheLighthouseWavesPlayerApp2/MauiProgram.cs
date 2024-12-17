using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TheLighthouseWavesPlayerApp2.Database;
using TheLighthouseWavesPlayerApp2.Services;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
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
        mauiAppBuilder.Services.AddSingleton<DatabaseService>();
        mauiAppBuilder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
        
        mauiAppBuilder.Services.AddSingleton<IBookmarkService, BookmarkService>();
        mauiAppBuilder.Services.AddSingleton<IPlaylistService, PlaylistService>();
        mauiAppBuilder.Services.AddSingleton<IVideoService, VideoService>();
        mauiAppBuilder.Services.AddSingleton<IUserSettingsService, UserSettingsService>();
        

        return mauiAppBuilder;        
    }

    public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder mauiAppBuilder)
    {
        // More view-models registered here.

        return mauiAppBuilder;        
    }

    public static MauiAppBuilder RegisterViews(this MauiAppBuilder mauiAppBuilder)
    {
        // More views registered here.

        return mauiAppBuilder;        
    }
}