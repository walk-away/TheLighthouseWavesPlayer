using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using Path = System.IO.Path;

#if ANDROID
using Android.Database;
using Android.Provider;
using Uri = Android.Net.Uri;
using Android.Graphics;
using Android.Media;
#endif

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class VideoDiscoveryService : IVideoDiscoveryService
{
    public async Task<bool> RequestPermissionsAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        if (Permissions.ShouldShowRationale<Permissions.StorageRead>())
        {
            await Shell.Current.DisplayAlert("Permission Needed", "Storage permission is required to find video files.", "OK");
        }

        status = await Permissions.RequestAsync<Permissions.StorageRead>();

        return status == PermissionStatus.Granted;
    }

    public async Task<IList<VideoInfo>> DiscoverVideosAsync()
    {
        if (!await RequestPermissionsAsync())
        {
            await Shell.Current.DisplayAlert("Permission Denied", "Cannot access videos without storage permission.", "OK");
            return new List<VideoInfo>();
        }

        var videoFiles = new List<VideoInfo>();

#if ANDROID
        await Task.Run(() =>
        {
            try
            {
                var context = Platform.CurrentActivity;
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("Error: Platform.CurrentActivity is null.");
                    return;
                }

                var contentResolver = context.ContentResolver;

                Uri? uri = MediaStore.Video.Media.ExternalContentUri;

                string[] projection = {
                    MediaStore.Video.Media.InterfaceConsts.Id,
                    MediaStore.Video.Media.InterfaceConsts.Data,
                    MediaStore.Video.Media.InterfaceConsts.Title,
                    MediaStore.Video.Media.InterfaceConsts.Duration
                };

                if (uri != null)
                {
                    ICursor? cursor = contentResolver?.Query(
                        uri,
                        projection,
                        null,
                        null,
                        MediaStore.Video.Media.InterfaceConsts.DateAdded + " DESC"
                    );

                    if (cursor != null)
                    {
                        int dataColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Data);
                        int titleColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Title);
                        int durationColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Duration);

                        if (cursor.MoveToFirst())
                        {
                            do
                            {
                                try
                                {
                                    if (dataColumn == -1 || titleColumn == -1 || durationColumn == -1)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Error: Column index not found.");
                                        continue;
                                    }

                                    string filePath = cursor.GetString(dataColumn) ?? string.Empty;
                                    string title = cursor.GetString(titleColumn) ?? string.Empty;
                                    long duration = cursor.GetLong(durationColumn);

                                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                                    {
                                        string? thumbnailPath = null;
                                        try
                                        {
                                            Bitmap? thumbnailBitmap = ThumbnailUtils.CreateVideoThumbnail(filePath, ThumbnailKind.MiniKind);

                                            if (thumbnailBitmap != null)
                                            {
                                                string thumbnailFileName = $"thumb_{Path.GetFileNameWithoutExtension(filePath)}_{Guid.NewGuid()}.jpg";
                                                string cachedThumbnailPath = Path.Combine(FileSystem.CacheDirectory, thumbnailFileName);

                                                using (var stream = new FileStream(cachedThumbnailPath, FileMode.Create))
                                                {
                                                    thumbnailBitmap.Compress(Bitmap.CompressFormat.Jpeg ?? throw new InvalidOperationException(), 80, stream); // Compress and save
                                                }

                                                thumbnailBitmap.Recycle();
                                                thumbnailPath = cachedThumbnailPath;
                                                System.Diagnostics.Debug.WriteLine($"Thumbnail generated: {thumbnailPath}");
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Failed to generate thumbnail for: {filePath}");
                                            }
                                        }
                                        catch (Exception thumbEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error generating thumbnail for {filePath}: {thumbEx.Message}");
                                        }

                                        videoFiles.Add(new VideoInfo
                                        {
                                            FilePath = filePath,
                                            Title = string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(filePath) : title,
                                            DurationMilliseconds = duration,
                                            ThumbnailPath = thumbnailPath // Assign the generated path
                                        });
                                    }
                                    else if (!string.IsNullOrEmpty(filePath))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"File not found or path empty: {filePath}");
                                    }
                                }
                                catch (Exception itemEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error processing video item: {itemEx.Message}");
                                }
                            } while (cursor.MoveToNext());
                        }

                        cursor.Close();
                    }
                }
            }
            catch (Exception queryEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error querying MediaStore: {queryEx.Message}");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Error", "Could not retrieve video list.", "OK");
                });
            }
        });
#else
        // Handle non-Android platforms
        System.Diagnostics.Debug.WriteLine("Video discovery with thumbnail generation is only implemented for Android.");
        await Task.CompletedTask;
        return new List<VideoInfo>();
#endif

        return videoFiles;
    }
}
