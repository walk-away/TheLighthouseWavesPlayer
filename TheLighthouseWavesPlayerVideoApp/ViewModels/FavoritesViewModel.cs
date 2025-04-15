using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels
{
    public partial class FavoritesViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;

        [ObservableProperty] private ObservableCollection<Video> _favoriteVideos;

        [ObservableProperty] private Video _selectedVideo;

        [ObservableProperty] private bool _isLoading;

        public FavoritesViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            FavoriteVideos = new ObservableCollection<Video>();

            LoadFavoritesCommand = new AsyncRelayCommand(LoadFavorites);
            PlayVideoCommand = new AsyncRelayCommand<Video>(PlayVideo);
            ToggleFavoriteCommand = new AsyncRelayCommand<Video>(ToggleFavorite);
            RemoveFromFavoritesCommand = new AsyncRelayCommand<Video>(RemoveFromFavorites);
        }

        public IAsyncRelayCommand LoadFavoritesCommand { get; }
        public IAsyncRelayCommand<Video> PlayVideoCommand { get; }
        public IAsyncRelayCommand<Video> ToggleFavoriteCommand { get; }
        public IAsyncRelayCommand<Video> RemoveFromFavoritesCommand { get; }

        public async Task LoadFavorites()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var favorites = await _databaseService.GetFavoriteVideosAsync();
                
                foreach (var video in favorites)
                {
                    Console.WriteLine($"Video: {video.Title}, Duration: {video.Duration}");
                    if (video.Duration.TotalSeconds < 1 && File.Exists(video.FilePath))
                    {
                        Console.WriteLine($"Video {video.Title} has zero duration but file exists");
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FavoriteVideos.Clear();
                    foreach (var video in favorites)
                    {
                        FavoriteVideos.Add(video);
                    }
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PlayVideo(Video video)
        {
            if (video == null)
                return;
            
            var navigationParameter = new Dictionary<string, object>
            {
                { "id", video.Id },
                { "path", video.FilePath }
            };
            
            await Shell.Current.GoToAsync("videoPlayer", navigationParameter);
        }

        private async Task ToggleFavorite(Video video)
        {
            if (video == null)
                return;

            await _databaseService.ToggleFavoriteAsync(video.Id);
            
            await RemoveFromFavorites(video);
        }
        
        private async Task RemoveFromFavorites(Video video)
        {
            if (video == null)
                return;
        
            try
            {
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    FavoriteVideos.Remove(video);
                });

                video.IsFavorite = false;
                await _databaseService.UpdateVideoAsync(video);
                
                await Toast.Make($"'{video.Title}' removed from favorites").Show();
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    if (!FavoriteVideos.Contains(video))
                        FavoriteVideos.Add(video);
                });
        
                await Shell.Current.DisplayAlert(
                    "Error", 
                    $"Failed to remove video: {ex.Message}", 
                    "OK");
            }
        }
    }
}