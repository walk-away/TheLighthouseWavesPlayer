using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute(nameof(VideoPlayerPage), typeof(VideoPlayerPage));
        Routing.RegisterRoute(nameof(VideoLibraryPage), typeof(VideoLibraryPage)); // Optional if already in Shell
        Routing.RegisterRoute(nameof(FavoritesPage), typeof(FavoritesPage)); // Optional if already in Shell
    }
}