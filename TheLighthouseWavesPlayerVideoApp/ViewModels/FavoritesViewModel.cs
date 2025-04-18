using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

// Needed for navigation

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public partial class FavoritesViewModel : BaseViewModel
{
    private readonly IFavoritesService _favoritesService;

    [ObservableProperty]
    ObservableCollection<VideoInfo> favoriteVideos;

    public FavoritesViewModel(IFavoritesService favoritesService)
    {
        _favoritesService = favoritesService;
        Title = "Favorites";
        FavoriteVideos = new ObservableCollection<VideoInfo>();
    }

    [RelayCommand]
    async Task LoadFavoritesAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            FavoriteVideos.Clear();
            var favs = await _favoritesService.GetFavoritesAsync();
            if (favs != null)
            {
                foreach (var video in favs)
                {
                    // Ensure file still exists before adding? Optional.
                    // if (File.Exists(video.FilePath))
                    // {
                         FavoriteVideos.Add(video);
                    // }
                    // else {
                         // Optionally remove favorite if file is gone
                         // await _favoritesService.RemoveFavoriteAsync(video);
                    // }
                }
            }
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

    // Command to handle item tapped/selected in the View
    [RelayCommand]
    async Task GoToDetailsAsync(VideoInfo video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath))
            return;

        // Navigate to the player page, passing the file path
        await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?FilePath={Uri.EscapeDataString(video.FilePath)}");
    }

    // Command to remove from favorites
    [RelayCommand]
    async Task RemoveFavoriteAsync(VideoInfo video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath)) return;

        try
        {
            await _favoritesService.RemoveFavoriteAsync(video);
            FavoriteVideos.Remove(video); // Update the collection immediately
            // Optionally provide feedback
            // await Shell.Current.DisplayAlert("Favorites", $"{video.Title} removed from favorites.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing favorite: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Could not remove favorite.", "OK");
        }
    }

    // Method called when the page appears
    public async Task OnAppearing()
    {
       await LoadFavoritesAsync();
    }
}