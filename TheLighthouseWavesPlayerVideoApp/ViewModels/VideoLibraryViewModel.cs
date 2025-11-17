using System.Collections.ObjectModel;
using System.ComponentModel;
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

    [ObservableProperty]
    private ObservableCollection<Playlist> _availablePlaylists = new();

    [ObservableProperty]
    private bool _isSelectingPlaylist;

    [ObservableProperty]
    private VideoInfo _videoForPlaylist = new VideoInfo();

    [ObservableProperty]
    private bool _hasPlaylists;

    [ObservableProperty]
    private ObservableCollection<VideoInfo> _allVideos = new();

    [ObservableProperty]
    private ObservableCollection<VideoInfo> _videos = new();

    [ObservableProperty]
    private VideoInfo _selectedVideo = new VideoInfo();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SortOption> _sortOptions = new();

    [ObservableProperty]
    private SortOption _selectedSortOption = new SortOption(string.Empty, string.Empty, true);

    private string _lastSelectedSortProperty;
    private bool _lastSelectedSortIsAscending;

    public VideoLibraryViewModel(
        IVideoDiscoveryService videoDiscoveryService,
        IFavoritesService favoritesService,
        ILocalizedResourcesProvider resourcesProvider,
        IPlaylistService playlistService)
    {
        _videoDiscoveryService =
            videoDiscoveryService ?? throw new ArgumentNullException(nameof(videoDiscoveryService));
        _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
        _resourcesProvider = resourcesProvider ?? throw new ArgumentNullException(nameof(resourcesProvider));
        _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
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
            filtered = filtered.Where(v => !string.IsNullOrEmpty(v.Title) &&
                                           v.Title.ToLowerInvariant().Contains(lower));
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
        {
            Videos.Add(video);
        }
    }

    [RelayCommand]
    public async Task LoadVideos()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var status = await _videoDiscoveryService.RequestPermissionsAsync();

            if (status == PermissionStatus.Granted)
            {
                AllVideos.Clear();
                Videos.Clear();

                var discovered = await _videoDiscoveryService.DiscoverVideosAsync();
                var favs = (await _favoritesService.GetFavoritesAsync())?.Select(f => f.FilePath).ToHashSet() ??
                           new HashSet<string>();

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
            else
            {
                var openSettings = await Shell.Current.DisplayAlert(
                    _resourcesProvider["Permissions_Storage_Title"],
                    _resourcesProvider["Permissions_Storage_Message"],
                    _resourcesProvider["Permissions_Open_Settings"],
                    _resourcesProvider["Button_OK"]);

                if (openSettings)
                {
                    AppInfo.ShowSettingsUI();
                }
            }
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
    private async Task GoToDetails(VideoInfo video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath))
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?FilePath={Uri.EscapeDataString(video.FilePath)}");
    }

    [RelayCommand]
    private async Task ToggleFavorite(VideoInfo? video)
    {
        if (video == null || string.IsNullOrEmpty(video.FilePath))
        {
            return;
        }

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
    private async Task ShowPlaylistSelection(VideoInfo video)
    {
        if (video == null)
        {
            return;
        }

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
    private async Task SavePlaylistSelection()
    {
        if (VideoForPlaylist == null)
        {
            return;
        }

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
    private void CancelPlaylistSelection()
    {
        IsSelectingPlaylist = false;
    }

    [RelayCommand]
    private async Task CheckPlaylistsExist()
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
