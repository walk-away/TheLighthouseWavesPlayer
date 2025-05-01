using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public partial class VideoLibraryViewModel : BaseViewModel
{
    private readonly IVideoDiscoveryService _videoDiscoveryService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILocalizedResourcesProvider _resourcesProvider;
    
    [ObservableProperty] ObservableCollection<VideoInfo> _allVideos;
    [ObservableProperty] ObservableCollection<VideoInfo> _videos;
    [ObservableProperty] VideoInfo _selectedVideo;
    [ObservableProperty] string _searchText = string.Empty;
    [ObservableProperty] ObservableCollection<SortOption> _sortOptions;
    [ObservableProperty] SortOption _selectedSortOption;

    public VideoLibraryViewModel(IVideoDiscoveryService videoDiscoveryService, IFavoritesService favoritesService, ILocalizedResourcesProvider resourcesProvider)
    {
        _videoDiscoveryService = videoDiscoveryService;
        _favoritesService = favoritesService;
        _resourcesProvider = resourcesProvider;
        Title = _resourcesProvider["Library_Title"];

        AllVideos = new ObservableCollection<VideoInfo>();
        Videos = new ObservableCollection<VideoInfo>();
        
        SortOptions = new ObservableCollection<SortOption>
        {
            new SortOption(_resourcesProvider["Sort_TitleAsc"], "Title", true),
            new SortOption(_resourcesProvider["Sort_TitleDesc"], "Title", false),
            new SortOption(_resourcesProvider["Sort_DurationAsc"], "DurationMilliseconds", true),
            new SortOption(_resourcesProvider["Sort_DurationDesc"], "DurationMilliseconds", false)
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
        IEnumerable<VideoInfo> filteredVideos = AllVideos;

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

        Videos.Clear();
        foreach (var video in filteredVideos)
        {
            Videos.Add(video);
        }
    }

    [RelayCommand]
    async Task LoadVideos()
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
                var favoritePaths = await _favoritesService.GetFavoritesAsync();
                var favoritePathsSet = new HashSet<string>(favoritePaths.Select(f => f.FilePath));

                foreach (var video in discoveredVideos)
                {
                    video.IsFavorite = favoritePathsSet.Contains(video.FilePath);
                    AllVideos.Add(video);
                }
            }

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

    [RelayCommand]
    async Task GoToDetails(VideoInfo video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath))
            return;

        await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?FilePath={Uri.EscapeDataString(video.FilePath)}");
    }

    [RelayCommand]
    async Task ToggleFavorite(VideoInfo video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath)) return;

        try
        {
            if (video.IsFavorite)
            {
                await _favoritesService.RemoveFavoriteAsync(video);
                video.IsFavorite = false;
                await Shell.Current.DisplayAlert("Favorites", $"{video.Title} removed from favorites.", "OK");
            }
            else
            {
                await _favoritesService.AddFavoriteAsync(video);
                video.IsFavorite = true;
                await Shell.Current.DisplayAlert("Favorites", $"{video.Title} added to favorites.", "OK");
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
        await LoadVideos();
    }
}