using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
// Required for MediaElementState

// If needed to store full VideoInfo

namespace TheLighthouseWavesPlayerVideoApp.ViewModels; // Ensure this namespace matches your project

// Apply QueryProperty to receive the FilePath from navigation
[QueryProperty(nameof(FilePath), "FilePath")]
public partial class VideoPlayerViewModel : BaseViewModel // Assuming you have BaseViewModel
{
    private readonly IFavoritesService _favoritesService;

    [ObservableProperty]
    string filePath; // The raw file path received via navigation

    [ObservableProperty]
    MediaSource videoSource; // Source for the MediaElement

    [ObservableProperty] // Add this property
    MediaElementState currentState = MediaElementState.None; // Track player state

    [ObservableProperty]
    bool isFavorite; // To show favorite status

    // Optional: Store the full VideoInfo if needed for title display etc.
    // [ObservableProperty]
    // VideoInfo currentVideo;

    public VideoPlayerViewModel(IFavoritesService favoritesService)
    {
        _favoritesService = favoritesService;
        Title = "Player";
    }

    // This method is called when the FilePath property is set by navigation
    async partial void OnFilePathChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            // Set the source for the MediaElement
            VideoSource = MediaSource.FromFile(value);
            Title = Path.GetFileName(value); // Set title based on file name

            // Check favorite status
            await CheckFavoriteStatusAsync(); // Call the method here

            // Optional: Load full VideoInfo if needed
            // This might require another service or passing more data
        }
        else // Handle case where FilePath becomes null/empty
        {
             VideoSource = null;
             Title = "Player";
             IsFavorite = false;
             CurrentState = MediaElementState.None;
        }
    }

    // Add this method
    public async Task CheckFavoriteStatusAsync()
    {
        if (!string.IsNullOrEmpty(FilePath))
        {
            IsFavorite = await _favoritesService.IsFavoriteAsync(FilePath);
        }
        else
        {
            IsFavorite = false;
        }
    }

    [RelayCommand]
    async Task ToggleFavoriteAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        try
        {
             // We need a VideoInfo object to add/remove.
             // If we only have the path, we might need to fetch/create one.
             // For simplicity, let's assume we can create a basic one here.
             // A better way is to pass the full VideoInfo object during navigation.
             var video = new VideoInfo { FilePath = this.FilePath, Title = Path.GetFileNameWithoutExtension(this.FilePath) }; // Basic info

            if (IsFavorite)
            {
                await _favoritesService.RemoveFavoriteAsync(video);
                IsFavorite = false;
                 // await Shell.Current.DisplayAlert("Favorites", "Removed from favorites.", "OK");
            }
            else
            {
                await _favoritesService.AddFavoriteAsync(video);
                IsFavorite = true;
                 // await Shell.Current.DisplayAlert("Favorites", "Added to favorites.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling favorite in player: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Could not update favorites.", "OK");
        }
    }

    // Note: Play/Pause/Stop commands can be handled directly by the MediaElement controls
    // If you need custom logic, add RelayCommands here and bind them.

    // Add this method for cleanup
    public void OnNavigatedFrom()
    {
        // Stop and release media resources
        // Setting Source to null might be enough if MediaElement handles it well.
        // Explicitly setting state helps ensure cleanup.
        VideoSource = null;
        CurrentState = MediaElementState.None; // Reset state in ViewModel
        System.Diagnostics.Debug.WriteLine("VideoPlayerViewModel.OnNavigatedFrom called.");
    }
}