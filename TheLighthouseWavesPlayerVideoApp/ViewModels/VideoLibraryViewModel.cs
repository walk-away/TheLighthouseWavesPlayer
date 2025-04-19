using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;
using System.Linq;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels
{
    public partial class VideoLibraryViewModel : BaseViewModel
    {
        private readonly IVideoDiscoveryService _videoDiscoveryService;
        private readonly IFavoritesService _favoritesService;

        [ObservableProperty] ObservableCollection<VideoInfo> allVideos;
        [ObservableProperty] ObservableCollection<VideoInfo> videos;
        [ObservableProperty] VideoInfo selectedVideo;

        [ObservableProperty] string searchText;

        [ObservableProperty] ObservableCollection<SortOption> sortOptions;
        [ObservableProperty] SortOption selectedSortOption;

        public VideoLibraryViewModel(IVideoDiscoveryService videoDiscoveryService, IFavoritesService favoritesService)
        {
            _videoDiscoveryService = videoDiscoveryService;
            _favoritesService = favoritesService;
            Title = "Video Library";
            
            // Initialize collections
            AllVideos = new ObservableCollection<VideoInfo>();
            Videos = new ObservableCollection<VideoInfo>();
            
            // Initialize sort options
            SortOptions = new ObservableCollection<SortOption>
            {
                new SortOption("Title (A-Z)", "Title", true),
                new SortOption("Title (Z-A)", "Title", false),
                new SortOption("Duration (Short-Long)", "DurationMilliseconds", true),
                new SortOption("Duration (Long-Short)", "DurationMilliseconds", false)
            };
            
            // Set default sort option
            SelectedSortOption = SortOptions[0];
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedSortOptionChanged(SortOption value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            // Create a new filtered and sorted collection
            IEnumerable<VideoInfo> filteredVideos = AllVideos;
            
            // Apply search filter if search text is provided
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLowerInvariant();
                filteredVideos = filteredVideos.Where(v => 
                    v.Title?.ToLowerInvariant().Contains(searchLower) == true);
            }
            
            // Apply sorting
            if (SelectedSortOption != null)
            {
                switch (SelectedSortOption.Property)
                {
                    case "Title":
                        filteredVideos = SelectedSortOption.IsAscending 
                            ? filteredVideos.OrderBy(v => v.Title)
                            : filteredVideos.OrderByDescending(v => v.Title);
                        break;
                    case "DurationMilliseconds":
                        filteredVideos = SelectedSortOption.IsAscending 
                            ? filteredVideos.OrderBy(v => v.DurationMilliseconds)
                            : filteredVideos.OrderByDescending(v => v.DurationMilliseconds);
                        break;
                }
            }
            
            // Update the Videos collection
            Videos.Clear();
            foreach (var video in filteredVideos)
            {
                Videos.Add(video);
            }
        }

        [RelayCommand]
        async Task LoadVideosAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                AllVideos.Clear();
                Videos.Clear();
                
                var discoveredVideos = await _videoDiscoveryService.DiscoverVideosAsync();
                if (discoveredVideos != null)
                {
                    foreach (var video in discoveredVideos)
                    {
                        AllVideos.Add(video);
                    }
                }
                
                // Apply filters to update the Videos collection
                ApplyFilters();
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
        void ClearSearch()
        {
            SearchText = string.Empty;
        }

        // Keep existing commands
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