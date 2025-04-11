using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Services;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels
{
    public partial class VideoLibraryViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IPermissionService _permissionService;
        private readonly IMediaService _mediaService;

        [ObservableProperty] private ObservableCollection<Video> _videos;

        [ObservableProperty] private Video _selectedVideo;

        [ObservableProperty] private bool _isLoading;

        public VideoLibraryViewModel(
            IDatabaseService databaseService,
            IPermissionService permissionService,
            IMediaService mediaService)
        {
            _databaseService = databaseService;
            _permissionService = permissionService;
            _mediaService = mediaService;
            Videos = new ObservableCollection<Video>();

            LoadVideosCommand = new AsyncRelayCommand(LoadVideos);
            ImportVideoCommand = new AsyncRelayCommand(ImportVideo);
            PlayVideoCommand = new AsyncRelayCommand<Video>(PlayVideo);
            ToggleFavoriteCommand = new AsyncRelayCommand<Video>(ToggleFavorite);
            DeleteVideoCommand = new AsyncRelayCommand<Video>(DeleteVideo);
        }

        public IAsyncRelayCommand LoadVideosCommand { get; }
        public IAsyncRelayCommand ImportVideoCommand { get; }
        public IAsyncRelayCommand<Video> PlayVideoCommand { get; }
        public IAsyncRelayCommand<Video> ToggleFavoriteCommand { get; }
        public IAsyncRelayCommand<Video> DeleteVideoCommand { get; }

        public async Task LoadVideos()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                // First check if we have permission
                bool hasPermission = await _permissionService.CheckAndRequestVideoPermissions();

                if (!hasPermission)
                {
                    // Show message to user about missing permissions
                    await Shell.Current.DisplayAlert(
                        "Permission Required",
                        "Storage permission is needed to access videos",
                        "OK");
                    return;
                }

                // Get videos from MediaStore (for scoped storage support)
                var mediaStoreVideos = await _mediaService.GetVideosFromMediaStore();

                // Also get videos from your database
                var savedVideos = await _databaseService.GetVideosAsync();

                // Combine and deduplicate if needed
                var allVideos = mediaStoreVideos.Concat(savedVideos)
                    .GroupBy(v => v.FilePath)
                    .Select(g => g.First())
                    .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Videos.Clear();
                    foreach (var video in allVideos)
                    {
                        Videos.Add(video);
                    }
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load videos: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ImportVideo()
        {
            try
            {
                IsBusy = true;
                await Shell.Current.DisplayAlert("Info", "Starting video import process...", "OK");

                bool hasPermission = await _permissionService.CheckAndRequestVideoPermissions();

                if (!hasPermission)
                {
                    await Shell.Current.DisplayAlert(
                        "Permission Required",
                        "Storage permission is needed to import videos",
                        "OK");
                    return;
                }
                
                try
                {
                    var customFileType = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.Android, new[] { "video/*" } }
                        });

                    var options = new PickOptions
                    {
                        PickerTitle = "Please select a video file",
                        FileTypes = customFileType,
                    };

                    var result = await FilePicker.Default.PickAsync(options);

                    if (result != null)
                    {
                        string fileName = Path.GetFileName(result.FullPath);

                        await Shell.Current.DisplayAlert("Debug", $"Selected file: {fileName}", "OK");

                        var video = new Video
                        {
                            Title = fileName,
                            FilePath = result.FullPath,
                            DateAdded = DateTime.Now,
                            IsFavorite = false
                        };

                        await _databaseService.AddVideoAsync(video);
                        await LoadVideos();

                        await Shell.Current.DisplayAlert("Success", "Video imported successfully!", "OK");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Info", "No file was selected", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", $"FilePicker error: {ex.Message}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PlayVideo(Video video)
        {
            if (video == null)
            {
                await Shell.Current.DisplayAlert("Error", "Cannot play: video is null", "OK");
                return;
            }
    
            try
            {
                await Shell.Current.DisplayAlert("Debug", $"Attempting to play video ID: {video.Id}, Path: {video.FilePath}", "OK");
        
                // Pass both ID and file path for better reliability
                var navigationParameter = new Dictionary<string, object>
                {
                    { "id", video.Id },
                    { "path", video.FilePath }
                };
        
                await Shell.Current.GoToAsync("videoPlayer", navigationParameter);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to navigate to player: {ex.Message}", "OK");
            }
        }

        private async Task ToggleFavorite(Video video)
        {
            if (video == null)
                return;

            await _databaseService.ToggleFavoriteAsync(video.Id);
            video.IsFavorite = !video.IsFavorite;
            
            int index = Videos.IndexOf(video);
            if (index >= 0)
            {
                Videos.Remove(video);
                Videos.Insert(index, video);
            }
        }

        private async Task DeleteVideo(Video video)
        {
            if (video == null)
                return;

            bool answer = await Shell.Current.DisplayAlert("Confirm",
                "Are you sure you want to delete this video?", "Yes", "No");

            if (answer)
            {
                await _databaseService.DeleteVideoAsync(video);
                Videos.Remove(video);
            }
        }
    }
}