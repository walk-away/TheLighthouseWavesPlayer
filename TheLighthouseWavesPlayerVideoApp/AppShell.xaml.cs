using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }
    
    private void RegisterRoutes()
    {
        Routing.RegisterRoute("videoPlayer", typeof(VideoPlayerPage));
        Routing.RegisterRoute("videoLibrary", typeof(VideoLibraryPage));
        Routing.RegisterRoute("favorites", typeof(FavoritesPage));
    }
}