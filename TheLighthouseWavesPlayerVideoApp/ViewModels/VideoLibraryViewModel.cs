using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels;

public partial class VideoLibraryViewModel : BaseViewModel, IDisposable
{
    private readonly IVideoDiscoveryService _videoDiscoveryService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILocalizedResourcesProvider _resourcesProvider;
    private readonly IPlaylistService _playlistService;

    [ObservableProperty] private ObservableCollection<Playlist> _availablePlaylists;
    [ObservableProperty] private bool _isSelectingPlaylist;
    [ObservableProperty] private VideoInfo _videoForPlaylist;
    [ObservableProperty] private bool _hasPlaylists;
    [ObservableProperty] ObservableCollection<VideoInfo> _allVideos;
    [ObservableProperty] ObservableCollection<VideoInfo> _videos;
    [ObservableProperty] VideoInfo _selectedVideo;
    [ObservableProperty] string _searchText = string.Empty;
    [ObservableProperty] ObservableCollection<SortOption> _sortOptions;
    [ObservableProperty] SortOption _selectedSortOption;
    private string _lastSelectedSortProperty;
    private bool _lastSelectedSortIsAscending;

    public VideoLibraryViewModel(IVideoDiscoveryService videoDiscoveryService,
        IFavoritesService favoritesService,
        ILocalizedResourcesProvider resourcesProvider,
        IPlaylistService playlistService)
    {
        _videoDiscoveryService = videoDiscoveryService;
        _favoritesService = favoritesService;
        _resourcesProvider = resourcesProvider;
        _playlistService = playlistService;
        Title = _resourcesProvider["Library_Title"];

        AllVideos = new ObservableCollection<VideoInfo>();
        Videos = new ObservableCollection<VideoInfo>();
        AvailablePlaylists = new ObservableCollection<Playlist>();

        _lastSelectedSortProperty = "Title";
        _lastSelectedSortIsAscending = true;

        InitializeSortOptions();

        if (resourcesProvider is ObservableObject observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
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
                                 o => o.Property == _lastSelectedSortProperty &&
                                      o.IsAscending == _lastSelectedSortIsAscending)
                             ?? SortOptions[0];
    }

    private void OnResourceProviderPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item")
        {
            Title = _resourcesProvider["Library_Title"];

            UpdateSortOptions();
        }
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
            System.Diagnostics.Debug.WriteLine(
                $"ToggleFavorite: Video {video.Title}, current status: {video.IsFavorite}");

            if (video.IsFavorite)
            {
                await _favoritesService.RemoveFavoriteAsync(video);
                video.IsFavorite = false;
                await Shell.Current.DisplayAlert("Favorites", $"{video.Title} removed from favorites.", "OK");
            }
            else
            {
                video.IsFavorite = true;
                await _favoritesService.AddFavoriteAsync(video);
                await Shell.Current.DisplayAlert("Favorites", $"{video.Title} added to favorites.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling favorite: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Could not update favorites.", "OK");
        }
    }

    [RelayCommand]
    async Task ShowPlaylistSelection(VideoInfo video)
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
                    _resourcesProvider["Playlists_NoPlaylists"],
                    _resourcesProvider["Playlists_CreateFirstMessage"],
                    _resourcesProvider["Button_OK"]);
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
            await Shell.Current.DisplayAlert(
                _resourcesProvider["Playlists_Error"],
                string.Format(_resourcesProvider["Playlists_ErrorMessage"], ex.Message),
                _resourcesProvider["Button_OK"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task SavePlaylistSelection()
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
                _resourcesProvider["Playlists_Success"],
                _resourcesProvider["Playlists_UpdateSuccess"],
                _resourcesProvider["Button_OK"]);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                _resourcesProvider["Playlists_Error"],
                string.Format(_resourcesProvider["Playlists_ErrorMessage"], ex.Message),
                _resourcesProvider["Button_OK"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    void CancelPlaylistSelection()
    {
        IsSelectingPlaylist = false;
    }

    [RelayCommand]
    async Task CheckPlaylistsExist()
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
        await LoadVideos();
        await CheckPlaylistsExist();
    }

    public void Dispose()
    {
        if (_resourcesProvider is ObservableObject observableProvider)
        {
            observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
        }
    }
}