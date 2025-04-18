using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels
{
    public partial class VideoLibraryViewModel : BaseViewModel
    {
        private readonly IVideoDiscoveryService _videoDiscoveryService;
        private readonly IFavoritesService _favoritesService;

        [ObservableProperty] ObservableCollection<VideoInfo> videos;

        [ObservableProperty] VideoInfo selectedVideo; // For handling selection if needed directly in VM

        public VideoLibraryViewModel(IVideoDiscoveryService videoDiscoveryService, IFavoritesService favoritesService)
        {
            _videoDiscoveryService = videoDiscoveryService;
            _favoritesService = favoritesService;
            Title = "Video Library";
            Videos = new ObservableCollection<VideoInfo>();
        }

        [RelayCommand]
        async Task LoadVideosAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                Videos.Clear();
                var discoveredVideos = await _videoDiscoveryService.DiscoverVideosAsync();
                if (discoveredVideos != null)
                {
                    foreach (var video in discoveredVideos)
                    {
                        Videos.Add(video);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading videos: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to load video library.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Command to handle item tapped/selected in the View
        [RelayCommand]
        async Task GoToDetailsAsync(VideoInfo video)
        {
            if (video == null || string.IsNullOrEmpty(video.FilePath))
                return;

            // Navigate to the player page, passing the file path
            await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?FilePath={Uri.EscapeDataString(video.FilePath)}");
        }

        // Command to toggle favorite status directly from the library list
        [RelayCommand]
        async Task ToggleFavoriteAsync(VideoInfo video)
        {
            if (video == null || string.IsNullOrEmpty(video.FilePath)) return;

            bool isCurrentlyFavorite = await _favoritesService.IsFavoriteAsync(video.FilePath);

            try
            {
                if (isCurrentlyFavorite)
                {
                    await _favoritesService.RemoveFavoriteAsync(video);
                    // Optionally provide feedback
                    // await Shell.Current.DisplayAlert("Favorites", $"{video.Title} removed from favorites.", "OK");
                }
                else
                {
                    await _favoritesService.AddFavoriteAsync(video);
                    // Optionally provide feedback
                    // await Shell.Current.DisplayAlert("Favorites", $"{video.Title} added to favorites.", "OK");
                }
                // We might need a way to update the UI element's visual state (e.g., the favorite icon)
                // This often requires the VideoInfo object itself to have an IsFavorite property
                // that the ViewModel updates after the service call. Let's modify VideoInfo slightly.
                // For now, we assume the UI might refresh or handle this visually.
                // A better approach involves an IsFavorite property on a wrapper ViewModel or VideoInfo itself.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling favorite: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Could not update favorites.", "OK");
            }
        }

        // Method called when the page appears
        public async Task OnAppearing()
        {
            await LoadVideosAsync();
        }
    }
}