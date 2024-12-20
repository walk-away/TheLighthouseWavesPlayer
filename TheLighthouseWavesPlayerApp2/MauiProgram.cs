using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using TheLighthouseWavesPlayerApp2.Database;
using TheLighthouseWavesPlayerApp2.Services;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;
using TheLighthouseWavesPlayerApp2.ViewModels;

namespace TheLighthouseWavesPlayerApp2;

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
        mauiAppBuilder.Services.AddSingleton<BookmarkViewModel>();
        mauiAppBuilder.Services.AddSingleton<PlaylistViewModel>();
        mauiAppBuilder.Services.AddSingleton<UserSettingsViewModel>();
        mauiAppBuilder.Services.AddSingleton<VideoViewModel>();

        return mauiAppBuilder;        
    }

    public static MauiAppBuilder RegisterViews(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddTransient<MainPage>();

        return mauiAppBuilder;        
    }
}