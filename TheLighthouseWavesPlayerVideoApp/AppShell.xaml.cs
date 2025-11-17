using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp;

public partial class AppShell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(VideoPlayerPage), typeof(VideoPlayerPage));
        Routing.RegisterRoute(nameof(VideoLibraryPage), typeof(VideoLibraryPage));
        Routing.RegisterRoute(nameof(FavoritesPage), typeof(FavoritesPage));
        Routing.RegisterRoute(nameof(PlaylistsPage), typeof(PlaylistsPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }
}
