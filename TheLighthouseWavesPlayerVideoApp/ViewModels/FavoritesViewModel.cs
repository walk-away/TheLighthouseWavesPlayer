using System.Collections.ObjectModel;
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
        }

        public IAsyncRelayCommand LoadFavoritesCommand { get; }
        public IAsyncRelayCommand<Video> PlayVideoCommand { get; }
        public IAsyncRelayCommand<Video> ToggleFavoriteCommand { get; }

        public async Task LoadFavorites()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var favorites = await _databaseService.GetFavoriteVideosAsync();

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

            await Shell.Current.GoToAsync($"videoPlayer?id={video.Id}");
        }

        private async Task ToggleFavorite(Video video)
        {
            if (video == null)
                return;

            await _databaseService.ToggleFavoriteAsync(video.Id);

            // Remove from favorites list
            MainThread.BeginInvokeOnMainThread(() => { FavoriteVideos.Remove(video); });
        }
    }
}