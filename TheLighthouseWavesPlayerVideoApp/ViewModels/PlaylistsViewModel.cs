using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Views;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels
{
    public partial class PlaylistsViewModel : BaseViewModel, IDisposable
    {
        private readonly IPlaylistService _playlistService;
        private readonly ILocalizedResourcesProvider _resourcesProvider;

        [ObservableProperty] private ObservableCollection<Playlist> _playlists;
        [ObservableProperty] private Playlist? _selectedPlaylist;
        [ObservableProperty] private string _newPlaylistName = string.Empty;
        [ObservableProperty] private string _newPlaylistDescription = string.Empty;
        [ObservableProperty] private bool _isAddingNewPlaylist;

        public PlaylistsViewModel(IPlaylistService playlistService, ILocalizedResourcesProvider resourcesProvider)
        {
            _playlistService = playlistService;
            _resourcesProvider = resourcesProvider;
            Title = _resourcesProvider["Playlists_Title"] ?? "Playlists";

            Playlists = new ObservableCollection<Playlist>();

            if (resourcesProvider is ObservableObject observableProvider)
            {
                observableProvider.PropertyChanged += OnResourceProviderPropertyChanged;
            }
        }

        private void OnResourceProviderPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item")
            {
                Title = _resourcesProvider["Playlists_Title"] ?? "Playlists";
            }
        }

        [RelayCommand]
        private async Task PlayPlaylist(Playlist playlist)
        {
            if (playlist == null || playlist.VideoCount <= 0) return;

            try
            {
                IsBusy = true;

                var videos = await _playlistService.GetPlaylistVideosAsync(playlist.Id);

                if (videos.Count == 0)
                {
                    await Shell.Current.DisplayAlert(
                        _resourcesProvider["Playlists_EmptyPlaylist"],
                        _resourcesProvider["Playlists_NoVideosToPlay"],
                        _resourcesProvider["Button_OK"]);
                    return;
                }

                await Shell.Current.GoToAsync($"{nameof(VideoPlayerPage)}?PlaylistId={playlist.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing playlist: {ex.Message}");
                await Shell.Current.DisplayAlert(
                    _resourcesProvider["Playlists_Error"],
                    _resourcesProvider["Playlists_ErrorPlaying"],
                    _resourcesProvider["Button_OK"]);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ShowAddNewPlaylist()
        {
            NewPlaylistName = string.Empty;
            NewPlaylistDescription = string.Empty;
            IsAddingNewPlaylist = true;
        }

        [RelayCommand]
        private void CancelAddNewPlaylist()
        {
            IsAddingNewPlaylist = false;
        }

        [RelayCommand]
        private async Task AddNewPlaylist()
        {
            if (string.IsNullOrWhiteSpace(NewPlaylistName))
            {
                await Shell.Current.DisplayAlert(
                    _resourcesProvider["Playlists_Error"],
                    _resourcesProvider["Playlists_EmptyName"],
                    _resourcesProvider["Button_OK"]);
                return;
            }

            try
            {
                await _playlistService.CreatePlaylistAsync(NewPlaylistName, NewPlaylistDescription);
                IsAddingNewPlaylist = false;
                await LoadPlaylists();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating playlist: {ex.Message}");
                await Shell.Current.DisplayAlert(
                    _resourcesProvider["Playlists_Error"],
                    _resourcesProvider["Playlists_ErrorCreating"],
                    _resourcesProvider["Button_OK"]);
            }
        }

        [RelayCommand]
        private async Task DeletePlaylist(Playlist playlist)
        {
            if (playlist == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                _resourcesProvider["Playlists_ConfirmDeleteTitle"],
                string.Format(_resourcesProvider["Playlists_ConfirmDeleteMessage"], playlist.Name),
                _resourcesProvider["Playlists_Yes"],
                _resourcesProvider["Playlists_No"]);

            if (!confirm) return;

            try
            {
                await _playlistService.DeletePlaylistAsync(playlist);
                Playlists.Remove(playlist);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting playlist: {ex.Message}");
                await Shell.Current.DisplayAlert(
                    _resourcesProvider["Playlists_Error"],
                    _resourcesProvider["Playlists_ErrorDeleting"],
                    _resourcesProvider["Button_OK"]);
            }
        }

        [RelayCommand]
        private async Task LoadPlaylists()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                Playlists.Clear();

                var loadedPlaylists = await _playlistService.GetPlaylistsAsync();
                foreach (var playlist in loadedPlaylists)
                {
                    Playlists.Add(playlist);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading playlists: {ex.Message}");
                await Shell.Current.DisplayAlert(
                    _resourcesProvider["Playlists_Error"],
                    _resourcesProvider["Playlists_ErrorLoading"],
                    _resourcesProvider["Button_OK"]);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ViewPlaylistDetails(Playlist playlist)
        {
            if (playlist == null) return;

            await Shell.Current.GoToAsync($"PlaylistDetailPage?PlaylistId={playlist.Id}");
        }

        private async Task RefreshPlaylistsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                var currentPlaylistIds = Playlists.Select(p => p.Id).ToList();
                int? selectedId = SelectedPlaylist?.Id;

                Playlists.Clear();

                var loadedPlaylists = await _playlistService.GetPlaylistsAsync();
                foreach (var playlist in loadedPlaylists)
                {
                    Playlists.Add(playlist);

                    if (selectedId.HasValue && playlist.Id == selectedId.Value)
                    {
                        SelectedPlaylist = playlist;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing playlists: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task OnAppearing()
        {
            await RefreshPlaylistsAsync();
        }

        public void Dispose()
        {
            if (_resourcesProvider is ObservableObject observableProvider)
            {
                observableProvider.PropertyChanged -= OnResourceProviderPropertyChanged;
            }
        }
    }
}
