using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public sealed partial class VideoLibraryViewModel : BaseViewModel, IDisposable
{
    private readonly IVideoDiscoveryService _videoDiscoveryService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILocalizedResourcesProvider _resourcesProvider;
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
    private ObservableCollection<VideoInfo> _allVideos = [];

    [ObservableProperty]
    private ObservableCollection<VideoInfo> _videos = [];

    [ObservableProperty]
    private VideoInfo _selectedVideo = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SortOption> _sortOptions = [];

    [ObservableProperty]
    private SortOption _selectedSortOption = new(string.Empty, string.Empty, true);

    private string _lastSelectedSortProperty;
    private bool _lastSelectedSortIsAscending;

    public VideoLibraryViewModel(
        IVideoDiscoveryService videoDiscoveryService,
        IFavoritesService favoritesService,
        ILocalizedResourcesProvider resourcesProvider,
        IPlaylistService playlistService)
    {
        ArgumentNullException.ThrowIfNull(videoDiscoveryService);
        ArgumentNullException.ThrowIfNull(favoritesService);
        ArgumentNullException.ThrowIfNull(resourcesProvider);
        ArgumentNullException.ThrowIfNull(playlistService);

        _videoDiscoveryService = videoDiscoveryService;
        _favoritesService = favoritesService;
        _resourcesProvider = resourcesProvider;
        _playlistService = playlistService;
        Title = _resourcesProvider["Library_Title"];

        _lastSelectedSortProperty = "Title";
        _lastSelectedSortIsAscending = true;

        InitializeSortOptions();

        if (_resourcesProvider is INotifyPropertyChanged notifier)
        {
            notifier.PropertyChanged += OnResourceProviderPropertyChanged;
        }
    }

    private void InitializeSortOptions()
    {
        SortOptions =
        [
            new SortOption(_resourcesProvider["Sort_TitleAsc"], "Title", true),
            new SortOption(_resourcesProvider["Sort_TitleDesc"], "Title", false),
            new SortOption(_resourcesProvider["Sort_DurationAsc"], "DurationMilliseconds", true),
            new SortOption(_resourcesProvider["Sort_DurationDesc"], "DurationMilliseconds", false)
        ];
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
        SelectedSortOption = SortOptions.FirstOrDefault(o =>
                                 o.Property == _lastSelectedSortProperty &&
                                 o.IsAscending == _lastSelectedSortIsAscending)
                             ?? SortOptions.First();
    }

    private void OnResourceProviderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item")
        {
            Title = _resourcesProvider["Library_Title"];
            UpdateSortOptions();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedSortOptionChanged(SortOption value)
    {
        _lastSelectedSortProperty = value.Property;
        _lastSelectedSortIsAscending = value.IsAscending;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<VideoInfo> filtered = AllVideos;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var lower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(v => !string.IsNullOrWhiteSpace(v.Title) &&
                                           v.Title.Contains(lower, StringComparison.OrdinalIgnoreCase));
        }

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

        Videos.Clear();
        foreach (var video in filtered)
            Videos.Add(video);
    }

    [RelayCommand]
    public async Task LoadVideosAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            AllVideos.Clear();
            Videos.Clear();

            var discovered = await _videoDiscoveryService.DiscoverVideosAsync();
            var favs = (await _favoritesService.GetFavoritesAsync())?.Select(f => f.FilePath).ToHashSet() ?? [];

            if (discovered != null)
            {
                foreach (var vid in discovered)
                {
                    vid.IsFavorite = favs.Contains(vid.FilePath);
                    AllVideos.Add(vid);
                }
            }

            ApplyFilters();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
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
                    _resourcesProvider["Favorites_Title"] ?? "Favorites",
                    $"{video.Title} {_resourcesProvider["Favorites_Removed"] ?? "removed from favorites"}.",
                    _resourcesProvider["Button_OK"] ?? "OK");
            }
            else
            {
                video.IsFavorite = true;
                await _favoritesService.AddFavoriteAsync(video);
                await Shell.Current.DisplayAlert(
                    _resourcesProvider["Favorites_Title"] ?? "Favorites",
                    $"{video.Title} {_resourcesProvider["Favorites_Added"] ?? "added to favorites"}.",
                    _resourcesProvider["Button_OK"] ?? "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling favorite: {ex.Message}");
            await Shell.Current.DisplayAlert(
                _resourcesProvider["Error_Title"] ?? "Error",
                _resourcesProvider["Error_Favorites_Update"] ?? "Could not update favorites.",
                _resourcesProvider["Button_OK"] ?? "OK");
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
                    _resourcesProvider["Playlists_NoPlaylists"] ?? "No Playlists",
                    _resourcesProvider["Playlists_CreateFirstMessage"] ?? "Create a playlist first",
                    _resourcesProvider["Button_OK"] ?? "OK");
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
                _resourcesProvider["Playlists_Error"] ?? "Error",
                string.Format(_resourcesProvider["Playlists_ErrorMessage"] ?? "Error: {0}", ex.Message),
                _resourcesProvider["Button_OK"] ?? "OK");
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
                _resourcesProvider["Playlists_Success"] ?? "Success",
                _resourcesProvider["Playlists_UpdateSuccess"] ?? "Playlists updated",
                _resourcesProvider["Button_OK"] ?? "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving playlist selection: {ex.Message}");
            await Shell.Current.DisplayAlert(
                _resourcesProvider["Playlists_Error"] ?? "Error",
                string.Format(_resourcesProvider["Playlists_ErrorMessage"] ?? "Error: {0}", ex.Message),
                _resourcesProvider["Button_OK"] ?? "OK");
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

        if (_resourcesProvider is ObservableObject observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
