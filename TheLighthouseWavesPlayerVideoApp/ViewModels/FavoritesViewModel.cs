using System.Collections.ObjectModel;
using System.ComponentModel;
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

    [ObservableProperty] private ObservableCollection<VideoInfo> _allFavoriteVideos = new();

    [ObservableProperty] private ObservableCollection<VideoInfo> _favoriteVideos = new();

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private ObservableCollection<SortOption> _sortOptions = new();

    [ObservableProperty] private SortOption _selectedSortOption = new SortOption(string.Empty, string.Empty, true);

    private string _lastSelectedSortProperty;
    private bool _lastSelectedSortIsAscending;

    public FavoritesViewModel(IFavoritesService favoritesService, ILocalizedResourcesProvider resourcesProvider)
    {
        _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
        _resourcesProvider = resourcesProvider ?? throw new ArgumentNullException(nameof(resourcesProvider));
        Title = _resourcesProvider["Favorites_Title"];

        _lastSelectedSortProperty = "Title";
        _lastSelectedSortIsAscending = true;

        InitializeSortOptions();

        if (_resourcesProvider is INotifyPropertyChanged observableProvider)
        {
            observableProvider.PropertyChanged += OnResourceProviderPropertyChanged;
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

        SelectedSortOption = SortOptions.First();
    }

    private void UpdateSortOptions()
    {
        _lastSelectedSortProperty = SelectedSortOption.Property;
        _lastSelectedSortIsAscending = SelectedSortOption.IsAscending;

        var newOptions = new ObservableCollection<SortOption>
        {
            new SortOption(_resourcesProvider["Sort_TitleAsc"], "Title", true),
            new SortOption(_resourcesProvider["Sort_TitleDesc"], "Title", false),
            new SortOption(_resourcesProvider["Sort_DurationAsc"], "DurationMilliseconds", true),
            new SortOption(_resourcesProvider["Sort_DurationDesc"], "DurationMilliseconds", false)
        };

        SortOptions = newOptions;
        SelectedSortOption = SortOptions.FirstOrDefault(o => o.Property == _lastSelectedSortProperty &&
                                                             o.IsAscending == _lastSelectedSortIsAscending)
                             ?? SortOptions.First();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedSortOptionChanged(SortOption value)
    {
        _lastSelectedSortProperty = value.Property;
        _lastSelectedSortIsAscending = value.IsAscending;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<VideoInfo> filtered = AllFavoriteVideos;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(v =>
                !string.IsNullOrEmpty(v.Title) && v.Title.ToLowerInvariant().Contains(searchLower));
        }

        if (SelectedSortOption != null)
        {
            filtered = SelectedSortOption.Property switch
            {
                "Title" => SelectedSortOption.IsAscending
                    ? filtered.OrderBy(v => v.Title)
                    : filtered.OrderByDescending(v => v.Title),
                "DurationMilliseconds" => SelectedSortOption.IsAscending
                    ? filtered.OrderBy(v => v.DurationMilliseconds)
                    : filtered.OrderByDescending(v => v.DurationMilliseconds),
                _ => filtered
            };
        }

        FavoriteVideos.Clear();
        foreach (var video in filtered)
        {
            FavoriteVideos.Add(video);
        }
    }

    public async Task OnAppearing()
    {
        await LoadFavoritesAsync();
    }

    [RelayCommand]
    private async Task LoadFavoritesAsync()
    {
        if (IsBusy)
        {
            return;
        }

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
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private async Task GoToDetailsAsync(VideoInfo? video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath))
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?FilePath={Uri.EscapeDataString(video.FilePath)}");
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync(VideoInfo? video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath))
        {
            return;
        }

        try
        {
            await _favoritesService.RemoveFavoriteAsync(video);
            var toRemove = AllFavoriteVideos.FirstOrDefault(v => v.FilePath == video.FilePath);
            if (toRemove != null)
            {
                AllFavoriteVideos.Remove(toRemove);
            }

            var filteredRemove = FavoriteVideos.FirstOrDefault(v => v.FilePath == video.FilePath);
            if (filteredRemove != null)
            {
                FavoriteVideos.Remove(filteredRemove);
            }

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
        if (_resourcesProvider is INotifyPropertyChanged observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
        }
    }

    private void OnResourceProviderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item")
        {
            Title = _resourcesProvider["Favorites_Title"];
            UpdateSortOptions();
        }
    }
}
