using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;
using System.Linq;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels
{
    public partial class FavoritesViewModel : BaseViewModel
    {
        private readonly IFavoritesService _favoritesService;

        [ObservableProperty]
        ObservableCollection<VideoInfo> allFavoriteVideos;
        
        [ObservableProperty]
        ObservableCollection<VideoInfo> favoriteVideos;
        
        [ObservableProperty]
        string searchText;
        
        [ObservableProperty]
        ObservableCollection<SortOption> sortOptions;
        
        [ObservableProperty]
        SortOption selectedSortOption;

        public FavoritesViewModel(IFavoritesService favoritesService)
        {
            _favoritesService = favoritesService;
            Title = "Favorites";
            
            // Initialize collections
            AllFavoriteVideos = new ObservableCollection<VideoInfo>();
            FavoriteVideos = new ObservableCollection<VideoInfo>();
            
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
            IEnumerable<VideoInfo> filteredVideos = AllFavoriteVideos;
            
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
            
            // Update the FavoriteVideos collection
            FavoriteVideos.Clear();
            foreach (var video in filteredVideos)
            {
                FavoriteVideos.Add(video);
            }
        }

        [RelayCommand]
        async Task LoadFavoritesAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                AllFavoriteVideos.Clear();
                FavoriteVideos.Clear();
                
                var favs = await _favoritesService.GetFavoritesAsync();
                if (favs != null)
                {
                    foreach (var video in favs)
                    {
                        AllFavoriteVideos.Add(video);
                    }
                }
                
                // Apply filters to update the FavoriteVideos collection
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading favorites: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to load favorites.", "OK");
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
        async Task RemoveFavoriteAsync(VideoInfo video)
        {
            if (video == null || string.IsNullOrEmpty(video.FilePath)) return;

            try
            {
                await _favoritesService.RemoveFavoriteAsync(video);
                
                // Remove from both collections
                var itemToRemove = AllFavoriteVideos.FirstOrDefault(v => v.FilePath == video.FilePath);
                if (itemToRemove != null)
                    AllFavoriteVideos.Remove(itemToRemove);
                
                var filteredItemToRemove = FavoriteVideos.FirstOrDefault(v => v.FilePath == video.FilePath);
                if (filteredItemToRemove != null)
                    FavoriteVideos.Remove(filteredItemToRemove);
                
                await Shell.Current.DisplayAlert("Favorites", $"{video.Title} removed from favorites.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing favorite: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Could not remove favorite.", "OK");
            }
        }
        
        public async Task OnAppearing()
        {
           await LoadFavoritesAsync();
        }
    }
}