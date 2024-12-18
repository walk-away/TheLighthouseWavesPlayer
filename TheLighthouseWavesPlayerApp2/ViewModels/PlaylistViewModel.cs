using System.Collections.ObjectModel;
using System.Windows.Input;
using TheLighthouseWavesPlayerApp2.Models;
using TheLighthouseWavesPlayerApp2.Services.Interfaces;

namespace TheLighthouseWavesPlayerApp2.ViewModels;

public class PlaylistViewModel : BaseViewModel
{
    private readonly IPlaylistService _playlistService;

    public ObservableCollection<Playlist> Playlists { get; } = new();

    public PlaylistViewModel(IPlaylistService playlistService)
    {
        _playlistService = playlistService;
        LoadPlaylistsCommand = new Command(async () => await LoadPlaylists());
    }

    public ICommand LoadPlaylistsCommand { get; }

    private async Task LoadPlaylists()
    {
        var playlists = await _playlistService.GetPlaylistsAsync();
        Playlists.Clear();
        foreach (var playlist in playlists)
        {
            Playlists.Add(playlist);
        }
    }
}