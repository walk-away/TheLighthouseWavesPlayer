using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public sealed partial class VideoLibraryViewModel : VideoFilterableSortableViewModel, IDisposable
{
    private readonly IVideoDiscoveryService _videoDiscoveryService;
    private readonly IFavoritesService _favoritesService;
    private readonly IPlaylistService _playlistService;
    private bool _isDisposed;

    [ObservableProperty]
    private ObservableCollection<Playlist> _availablePlaylists = [];

    [ObservableProperty]
    private bool _isSelectingPlaylist;

    [ObservableProperty]
    private VideoInfo _videoForPlaylist = new();

    [ObservableProperty]
    private bool _hasPlaylists;

    [ObservableProperty]
    private VideoInfo _selectedVideo = new();

    public ObservableCollection<VideoInfo> AllVideos => AllItems;
    public ObservableCollection<VideoInfo> Videos => FilteredItems;

    public VideoLibraryViewModel(
        IVideoDiscoveryService videoDiscoveryService,
        IFavoritesService favoritesService,
        ILocalizedResourcesProvider resourcesProvider,
        IPlaylistService playlistService)
        : base(resourcesProvider)
    {
        ArgumentNullException.ThrowIfNull(videoDiscoveryService);
        ArgumentNullException.ThrowIfNull(favoritesService);
        ArgumentNullException.ThrowIfNull(playlistService);

        _videoDiscoveryService = videoDiscoveryService;
        _favoritesService = favoritesService;
        _playlistService = playlistService;
        Title = ResourcesProvider["Library_Title"];

        if (resourcesProvider is INotifyPropertyChanged notifier)
        {
            notifier.PropertyChanged += OnResourceProviderPropertyChanged;
        }
    }

    private void OnResourceProviderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item")
        {
            Title = ResourcesProvider["Library_Title"];
            UpdateSortOptions();
        }
    }

    [RelayCommand]
    public async Task LoadVideosAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            AllItems.Clear();
            FilteredItems.Clear();

            var discovered = await _videoDiscoveryService.DiscoverVideosAsync();
            var favs = (await _favoritesService.GetFavoritesAsync())?.Select(f => f.FilePath).ToHashSet() ?? [];

            if (discovered != null)
            {
                foreach (var vid in discovered)
                {
                    vid.IsFavorite = favs.Contains(vid.FilePath);
                    AllItems.Add(vid);
                }
            }

            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToDetailsAsync(VideoInfo video)
    {
        if (video == null || string.IsNullOrWhiteSpace(video.FilePath))
            return;

        await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?FilePath={Uri.EscapeDataString(video.FilePath)}");
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(VideoInfo? video)
    {
        if (video == null || string.IsNullOrWhiteSpace(video.FilePath)) return;

        try
        {
            System.Diagnostics.Debug.WriteLine(
                $"ToggleFavorite: Video {video.Title}, current status: {video.IsFavorite}");

            if (video.IsFavorite)
            {
                await _favoritesService.RemoveFavoriteAsync(video);
                video.IsFavorite = false;
                await Shell.Current.DisplayAlert(
                    ResourcesProvider["Favorites_Title"] ?? "Favorites",
                    $"{video.Title} {ResourcesProvider["Favorites_Removed"] ?? "removed from favorites"}.",
                    ResourcesProvider["Button_OK"] ?? "OK");
            }
            else
            {
                video.IsFavorite = true;
                await _favoritesService.AddFavoriteAsync(video);
                await Shell.Current.DisplayAlert(
                    ResourcesProvider["Favorites_Title"] ?? "Favorites",
                    $"{video.Title} {ResourcesProvider["Favorites_Added"] ?? "added to favorites"}.",
                    ResourcesProvider["Button_OK"] ?? "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling favorite: {ex.Message}");
            await Shell.Current.DisplayAlert(
                ResourcesProvider["Error_Title"] ?? "Error",
                ResourcesProvider["Error_Favorites_Update"] ?? "Could not update favorites.",
                ResourcesProvider["Button_OK"] ?? "OK");
        }
    }

    [RelayCommand]
    private async Task ShowPlaylistSelectionAsync(VideoInfo video)
    {
        if (video == null) return;

        try
        {
            IsBusy = true;
            VideoForPlaylist = video;

            var playlists = await _playlistService.GetPlaylistsAsync();
            HasPlaylists = playlists.Count > 0;

            if (!HasPlaylists)
            {
                await Shell.Current.DisplayAlert(
                    ResourcesProvider["Playlists_NoPlaylists"] ?? "No Playlists",
                    ResourcesProvider["Playlists_CreateFirstMessage"] ?? "Create a playlist first",
                    ResourcesProvider["Button_OK"] ?? "OK");
                return;
            }

            AvailablePlaylists.Clear();

            foreach (var playlist in playlists)
            {
                playlist.IsSelected = await _playlistService.IsVideoInPlaylistAsync(playlist.Id, video.FilePath);
                AvailablePlaylists.Add(playlist);
            }

            IsSelectingPlaylist = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing playlist selection: {ex.Message}");
            await Shell.Current.DisplayAlert(
                ResourcesProvider["Playlists_Error"] ?? "Error",
                string.Format(ResourcesProvider["Playlists_ErrorMessage"] ?? "Error: {0}", ex.Message),
                ResourcesProvider["Button_OK"] ?? "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SavePlaylistSelectionAsync()
    {
        if (VideoForPlaylist == null) return;

        try
        {
            IsBusy = true;
            System.Diagnostics.Debug.WriteLine(
                $"Saving playlist selection for {VideoForPlaylist.Title}, favorite status: {VideoForPlaylist.IsFavorite}");

            foreach (var playlist in AvailablePlaylists)
            {
                bool isInPlaylist = await _playlistService.IsVideoInPlaylistAsync(
                    playlist.Id, VideoForPlaylist.FilePath);

                if (playlist.IsSelected && !isInPlaylist)
                {
                    await _playlistService.AddVideoToPlaylistAsync(playlist.Id, VideoForPlaylist);
                }
                else if (!playlist.IsSelected && isInPlaylist)
                {
                    await _playlistService.RemoveVideoFromPlaylistAsync(
                        playlist.Id, VideoForPlaylist.FilePath);
                }
            }

            IsSelectingPlaylist = false;
            await Shell.Current.DisplayAlert(
                ResourcesProvider["Playlists_Success"] ?? "Success",
                ResourcesProvider["Playlists_UpdateSuccess"] ?? "Playlists updated",
                ResourcesProvider["Button_OK"] ?? "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving playlist selection: {ex.Message}");
            await Shell.Current.DisplayAlert(
                ResourcesProvider["Playlists_Error"] ?? "Error",
                string.Format(ResourcesProvider["Playlists_ErrorMessage"] ?? "Error: {0}", ex.Message),
                ResourcesProvider["Button_OK"] ?? "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelPlaylistSelection()
    {
        IsSelectingPlaylist = false;
    }

    [RelayCommand]
    private async Task CheckPlaylistsExistAsync()
    {
        try
        {
            var playlists = await _playlistService.GetPlaylistsAsync();
            HasPlaylists = playlists.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking playlists: {ex.Message}");
            HasPlaylists = false;
        }
    }

    public async Task OnAppearing()
    {
        await LoadVideosAsync();
        await CheckPlaylistsExistAsync();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        if (ResourcesProvider is INotifyPropertyChanged observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
