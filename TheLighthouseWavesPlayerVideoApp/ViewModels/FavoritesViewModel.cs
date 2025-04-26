using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public partial class FavoritesViewModel : BaseViewModel
{
    private readonly IFavoritesService _favoritesService;

    [ObservableProperty] ObservableCollection<VideoInfo> _allFavoriteVideos;
    [ObservableProperty] ObservableCollection<VideoInfo> _favoriteVideos;
    [ObservableProperty] string _searchText;
    [ObservableProperty] ObservableCollection<SortOption> _sortOptions;
    [ObservableProperty] SortOption _selectedSortOption;

    public FavoritesViewModel(IFavoritesService favoritesService)
    {
        _favoritesService = favoritesService;
        Title = "Favorites";

        AllFavoriteVideos = new ObservableCollection<VideoInfo>();
        FavoriteVideos = new ObservableCollection<VideoInfo>();

        SortOptions = new ObservableCollection<SortOption>
        {
            new SortOption("Title (A-Z)", "Title", true),
            new SortOption("Title (Z-A)", "Title", false),
            new SortOption("Duration (Short-Long)", "DurationMilliseconds", true),
            new SortOption("Duration (Long-Short)", "DurationMilliseconds", false)
        };

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
        IEnumerable<VideoInfo> filteredVideos = AllFavoriteVideos;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string searchLower = SearchText.ToLowerInvariant();
            filteredVideos = filteredVideos.Where(v =>
                v.Title?.ToLowerInvariant().Contains(searchLower) == true);
        }

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

        FavoriteVideos.Clear();
        foreach (var video in filteredVideos)
        {
            FavoriteVideos.Add(video);
        }
    }
    
    public async Task OnAppearing()
    {
        await LoadFavoritesAsync();
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
}