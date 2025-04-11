using TheLighthouseWavesPlayerVideoApp.Data;
using TheLighthouseWavesPlayerVideoApp.Models;
using TheLighthouseWavesPlayerVideoApp.Services;

#if ANDROID
using Android.Database;
using Android.Provider;
using Uri = Android.Net.Uri;
#endif

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class MediaService : IMediaService
{
    private readonly IDatabaseService _databaseService;
    private readonly IPermissionService _permissionService;

    public MediaService(IDatabaseService databaseService, IPermissionService permissionService)
    {
        _databaseService = databaseService;
        _permissionService = permissionService;
    }

    public async Task<List<Video>> GetVideosFromMediaStore()
    {
        if (!await _permissionService.CheckAndRequestVideoPermissions())
        {
            return new List<Video>();
        }

        var videos = new List<Video>();

        // This is Android-specific code, so we need to use platform-specific implementation
#if ANDROID
        try
        {
            var context = Platform.CurrentActivity;
            var contentResolver = context.ContentResolver;
            
            Uri? uri = MediaStore.Video.Media.ExternalContentUri;
            
            string[] projection =
            {
                MediaStore.Video.Media.InterfaceConsts.Id,
                MediaStore.Video.Media.InterfaceConsts.Title,
                MediaStore.Video.Media.InterfaceConsts.Data,
                MediaStore.Video.Media.InterfaceConsts.DateAdded,
                MediaStore.Video.Media.InterfaceConsts.Duration
            };
            
            ICursor cursor = contentResolver.Query(uri, projection, null, null, null);

            if (cursor != null && cursor.MoveToFirst())
            {
                do
                {
                    int idColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Id);
                    int titleColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Title);
                    int dataColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Data);
                    int dateColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateAdded);
                    int durationColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Duration);
                    
                    var video = new Video
                    {
                        Title = cursor.GetString(titleColumn),
                        FilePath = cursor.GetString(dataColumn),
                        DateAdded = DateTimeOffset.FromUnixTimeSeconds(cursor.GetLong(dateColumn)).DateTime,
                        Duration = TimeSpan.FromMilliseconds(cursor.GetLong(durationColumn)),
                        IsFavorite = false
                    };

                    videos.Add(video);
                } while (cursor.MoveToNext());

                cursor.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading videos: {ex.Message}");
        }
#endif

        return videos;
    }

    public async Task<Video> GetVideoById(string id)
    {
        return await _databaseService.GetByIdAsync(int.Parse(id));
    }

    public async Task<bool> ImportExternalVideo()
    {
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

                var video = new Video
                {
                    Title = fileName,
                    FilePath = result.FullPath,
                    DateAdded = DateTime.Now,
                    IsFavorite = false
                };

                await _databaseService.AddVideoAsync(video);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing video: {ex.Message}");
            return false;
        }
    }
}