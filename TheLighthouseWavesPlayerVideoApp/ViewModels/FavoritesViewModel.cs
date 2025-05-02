using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public partial class FavoritesViewModel : BaseViewModel, IDisposable
{
    private readonly IFavoritesService _favoritesService;
    private readonly ILocalizedResourcesProvider _resourcesProvider;

    [ObservableProperty] ObservableCollection<VideoInfo> _allFavoriteVideos;
    [ObservableProperty] ObservableCollection<VideoInfo> _favoriteVideos;
    [ObservableProperty] string _searchText;
    [ObservableProperty] ObservableCollection<SortOption> _sortOptions;
    [ObservableProperty] SortOption _selectedSortOption;
    private string _lastSelectedSortProperty;
    private bool _lastSelectedSortIsAscending;

    public FavoritesViewModel(IFavoritesService favoritesService, ILocalizedResourcesProvider resourcesProvider)
    {
        _favoritesService = favoritesService;
        _resourcesProvider = resourcesProvider;
        Title = _resourcesProvider["Favorites_Title"];

        AllFavoriteVideos = new ObservableCollection<VideoInfo>();
        FavoriteVideos = new ObservableCollection<VideoInfo>();

        _lastSelectedSortProperty = "Title";
        _lastSelectedSortIsAscending = true;

        InitializeSortOptions();
        
        if (resourcesProvider is ObservableObject observableProvider)
        {
            observableProvider.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "Item")
                {
                    Title = _resourcesProvider["Favorites_Title"];
                    
                    UpdateSortOptions();
                }
            };
        }
    }
    
    private void InitializeSortOptions()
    {
        SortOptions = new ObservableCollection<SortOption>
        {
            new SortOption(_resourcesProvider["Sort_TitleAsc"], "Title", true),
            new SortOption(_resourcesProvider["Sort_TitleDesc"], "Title", false),
            new SortOption(_resourcesProvider["Sort_DurationAsc"], "DurationMilliseconds", true),
            new SortOption(_resourcesProvider["Sort_DurationDesc"], "DurationMilliseconds", false)
        };

        SelectedSortOption = SortOptions[0];
    }
    
    private void UpdateSortOptions()
    {
        if (SelectedSortOption != null)
        {
            _lastSelectedSortProperty = SelectedSortOption.Property;
            _lastSelectedSortIsAscending = SelectedSortOption.IsAscending;
        }
        
        var newSortOptions = new ObservableCollection<SortOption>
        {
            new SortOption(_resourcesProvider["Sort_TitleAsc"], "Title", true),
            new SortOption(_resourcesProvider["Sort_TitleDesc"], "Title", false),
            new SortOption(_resourcesProvider["Sort_DurationAsc"], "DurationMilliseconds", true),
            new SortOption(_resourcesProvider["Sort_DurationDesc"], "DurationMilliseconds", false)
        };
        
        SortOptions = newSortOptions;
        
        SelectedSortOption = SortOptions.FirstOrDefault(
                                 o => o.Property == _lastSelectedSortProperty && o.IsAscending == _lastSelectedSortIsAscending) 
                             ?? SortOptions[0];
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedSortOptionChanged(SortOption value)
    {
        if (value != null)
        {
            _lastSelectedSortProperty = value.Property;
            _lastSelectedSortIsAscending = value.IsAscending;
        }
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
    
    public void Dispose()
    {
        if (_resourcesProvider is ObservableObject observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
        }
    }

    private void OnResourceProviderPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item")
        {
            Title = _resourcesProvider["Favorites_Title"];
            UpdateSortOptions();
        }
    }
}