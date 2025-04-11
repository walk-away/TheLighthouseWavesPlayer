using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Models;

namespace TheLighthouseWavesPlayerVideoApp.ViewModels
{
    [QueryProperty(nameof(VideoId), "id")]
    [QueryProperty(nameof(FilePath), "path")]
    public partial class VideoPlayerViewModel : BaseViewModel
    {
        private string _filePath;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                Debug.WriteLine($"FilePath property set to: {value}");

                if (string.IsNullOrEmpty(value))
                    return;

                // If we have a file path but no video yet, load from path
                if (Video == null)
                {
                    MainThread.BeginInvokeOnMainThread(async () => { await LoadVideoFromPathAsync(value); });
                }
            }
        }

        private readonly IDatabaseService _databaseService;

        [ObservableProperty] private Video _video;

        [ObservableProperty] private bool _isPlaying;

        [ObservableProperty] private double _position;

        [ObservableProperty] private double _duration;

        [ObservableProperty] private bool _isFullScreen;

        [ObservableProperty] private Video? _currentVideo;

        private int _videoId;

        public int VideoId
        {
            get => _videoId;
            set
            {
                _videoId = value;
                Debug.WriteLine($"VideoId property set to: {value}");

                // Don't use Task.Run here - it can cause issues with property change notifications
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await LoadVideoAsync(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in VideoId setter: {ex}");
                        await Shell.Current.DisplayAlert("Error", $"Failed to load video: {ex.Message}", "OK");
                    }
                });
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSkipForward))]
        [NotifyPropertyChangedFor(nameof(CanSkipBackward))]
        private double _currentPosition;

        public bool CanSkipForward => CurrentPosition < Duration - 10;
        public bool CanSkipBackward => CurrentPosition > 10;

        [RelayCommand(CanExecute = nameof(CanSkipForward))]
        private void SkipForward()
        {
            CurrentPosition = Math.Min(Duration, CurrentPosition + 10);
        }

        public VideoPlayerViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;

            TogglePlayPauseCommand = new RelayCommand(TogglePlayPause);
            ToggleFavoriteCommand = new AsyncRelayCommand(ToggleFavorite);
            ToggleFullScreenCommand = new RelayCommand(ToggleFullScreen);
            UpdatePositionCommand = new AsyncRelayCommand<double>(UpdatePosition);
        }

        public IRelayCommand TogglePlayPauseCommand { get; }
        public IAsyncRelayCommand ToggleFavoriteCommand { get; }
        public IRelayCommand ToggleFullScreenCommand { get; }
        public IAsyncRelayCommand<double> UpdatePositionCommand { get; }

        private async Task LoadVideoFromPathAsync(string path)
        {
            try
            {
                Debug.WriteLine($"Loading video from path: {path}");

                // Check if file exists
                if (!File.Exists(path))
                {
                    Debug.WriteLine($"File does not exist: {path}");
                    await Shell.Current.DisplayAlert("Error", "Video file not found", "OK");
                    return;
                }

                // First check if it's already in the database by path
                var existingVideo = await _databaseService.GetVideoByPathAsync(path);

                if (existingVideo != null)
                {
                    Debug.WriteLine($"Found existing video in database: {existingVideo.Title}");
                    Video = existingVideo;
                    IsPlaying = true;
                    Position = existingVideo.LastPosition.TotalSeconds;
                    return;
                }

                // If not in database, create a new video object
                var fileName = Path.GetFileName(path);
                Debug.WriteLine($"Creating new video object for: {fileName}");

                var video = new Video
                {
                    Title = fileName,
                    FilePath = path,
                    DateAdded = DateTime.Now,
                    IsFavorite = false,
                    Duration = TimeSpan.Zero, // Will be updated when media loads
                    LastPosition = TimeSpan.Zero
                };

                // Optionally add to database for future reference
                await _databaseService.AddVideoAsync(video);

                Video = video;
                IsPlaying = true;
                Position = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading video from path: {ex}");
                await Shell.Current.DisplayAlert("Error", $"Failed to load video: {ex.Message}", "OK");
            }
        }

        // Keep your existing LoadVideoAsync method, but modify it to fall back to file path if needed
        private async Task LoadVideoAsync(int id)
        {
            // Existing implementation...

            // If video wasn't found in the database and we have a file path, try that
            if (Video == null && !string.IsNullOrEmpty(FilePath))
            {
                await LoadVideoFromPathAsync(FilePath);
            }
        }


        private void TogglePlayPause()
        {
            IsPlaying = !IsPlaying;
        }

        private async Task ToggleFavorite()
        {
            if (Video == null)
                return;

            await _databaseService.ToggleFavoriteAsync(Video.Id);
            Video.IsFavorite = !Video.IsFavorite;
            OnPropertyChanged(nameof(TheLighthouseWavesPlayerVideoApp.ViewModels.VideoPlayerViewModel.Video));
        }

        private void ToggleFullScreen()
        {
            IsFullScreen = !IsFullScreen;
        }

        private async Task UpdatePosition(double position)
        {
            if (Video == null)
                return;

            Position = position;
            Video.LastPosition = TimeSpan.FromSeconds(position);
            Video.LastPlayed = DateTime.Now;

            await _databaseService.UpdateVideoAsync(Video);
        }

        public async Task SaveCurrentPosition(double position, double duration)
        {
            if (Video == null)
                return;

            Video.LastPosition = TimeSpan.FromSeconds(position);
            Video.Duration = TimeSpan.FromSeconds(duration);
            Video.LastPlayed = DateTime.Now;

            await _databaseService.UpdateVideoAsync(Video);
        }
    }
}