using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public sealed partial class FavoritesViewModel : VideoFilterableSortableViewModel, IDisposable
{
    private readonly IFavoritesService _favoritesService;
    private bool _disposed;

    public ObservableCollection<VideoInfo> AllFavoriteVideos => AllItems;
    public ObservableCollection<VideoInfo> FavoriteVideos => FilteredItems;

    public FavoritesViewModel(IFavoritesService favoritesService, ILocalizedResourcesProvider resourcesProvider)
        : base(resourcesProvider)
    {
        ArgumentNullException.ThrowIfNull(favoritesService);

        _favoritesService = favoritesService;
        Title = ResourcesProvider["Favorites_Title"];

        if (resourcesProvider is INotifyPropertyChanged notifier)
        {
            notifier.PropertyChanged += OnResourceProviderPropertyChanged;
        }
    }

    private void OnResourceProviderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item")
        {
            Title = ResourcesProvider["Favorites_Title"];
            UpdateSortOptions();
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
            return;

        IsBusy = true;
        try
        {
            AllItems.Clear();
            FilteredItems.Clear();

            var favs = await _favoritesService.GetFavoritesAsync();
            if (favs != null)
            {
                foreach (var video in favs)
                {
                    AllItems.Add(video);
                }
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading favorites: {ex.Message}");
            await ShowErrorAsync("Failed to load favorites.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToDetailsAsync(VideoInfo? video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath))
            return;

        await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?FilePath={Uri.EscapeDataString(video.FilePath)}");
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync(VideoInfo? video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath))
            return;

        try
        {
            await _favoritesService.RemoveFavoriteAsync(video);

            var toRemove = AllItems.FirstOrDefault(v => v.FilePath == video.FilePath);
            if (toRemove != null)
                AllItems.Remove(toRemove);

            var filteredRemove = FilteredItems.FirstOrDefault(v => v.FilePath == video.FilePath);
            if (filteredRemove != null)
                FilteredItems.Remove(filteredRemove);

            await Shell.Current.DisplayAlert("Favorites", $"{video.Title} removed from favorites.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing favorite: {ex.Message}");
            await ShowErrorAsync("Could not remove favorite.");
        }
    }

    private static async Task ShowErrorAsync(string message)
    {
        await Shell.Current.DisplayAlert("Error", message, "OK");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (ResourcesProvider is INotifyPropertyChanged observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
