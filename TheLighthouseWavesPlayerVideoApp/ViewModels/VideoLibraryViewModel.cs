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

        [ObservableProperty] VideoInfo selectedVideo;

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

        [RelayCommand]
        async Task GoToDetailsAsync(VideoInfo video)
        {
            if (video == null || string.IsNullOrEmpty(video.FilePath))
                return;

            await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?FilePath={Uri.EscapeDataString(video.FilePath)}");
        }

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
                }
                else
                {
                    await _favoritesService.AddFavoriteAsync(video);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling favorite: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Could not update favorites.", "OK");
            }
        }

        public async Task OnAppearing()
        {
            await LoadVideosAsync();
        }
    }
}