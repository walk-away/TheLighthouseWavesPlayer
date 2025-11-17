using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;
using Path = System.IO.Path;

#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using Uri = Android.Net.Uri;
using Android.Graphics;
using Android.Media;
#endif

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class VideoDiscoveryService : IVideoDiscoveryService
{
    public async Task<PermissionStatus> RequestPermissionsAsync()
    {
        PermissionStatus status;
        if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
        {
            status = await Permissions.CheckStatusAsync<Permissions.Media>();
            if (status == PermissionStatus.Granted)
            {
                return status;
            }

            if (status == PermissionStatus.Denied)
            {
                return status;
            }

            status = await Permissions.RequestAsync<Permissions.Media>();
        }
        else
        {
            status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
            if (status == PermissionStatus.Granted)
            {
                return status;
            }

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                return status;
            }

            status = await Permissions.RequestAsync<Permissions.StorageRead>();
        }

        return status;
    }

    [Obsolete("Obsolete")]
    public async Task<IList<VideoInfo>> DiscoverVideosAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Media>();
        if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major < 13)
        {
            status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        }

        if (status != PermissionStatus.Granted)
        {
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

                string[] projection =
                {
                    MediaStore.Video.Media.InterfaceConsts.Id, MediaStore.Video.Media.InterfaceConsts.Data,
                    MediaStore.Video.Media.InterfaceConsts.Title, MediaStore.Video.Media.InterfaceConsts.Duration,
                    MediaStore.Video.Media.InterfaceConsts.DisplayName
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
                        int idColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Id);
                        int dataColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Data);
                        int titleColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Title);
                        int durationColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Duration);
                        int displayNameColumn =
                            cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DisplayName);

                        if (cursor.MoveToFirst())
                        {
                            do
                            {
                                try
                                {
                                    if (idColumn == -1 || dataColumn == -1)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Error: Required column index not found.");
                                        continue;
                                    }

                                    long id = cursor.GetLong(idColumn);
                                    string filePath = cursor.GetString(dataColumn) ?? string.Empty;
                                    string title = titleColumn != -1
                                        ? cursor.GetString(titleColumn) ?? string.Empty
                                        : string.Empty;
                                    string displayName = displayNameColumn != -1
                                        ? cursor.GetString(displayNameColumn) ?? string.Empty
                                        : string.Empty;
                                    long durationFromMediaStore =
                                        durationColumn != -1 ? cursor.GetLong(durationColumn) : 0;

                                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Skipping invalid file path: {filePath}");
                                        continue;
                                    }

                                    long duration = GetAccurateDuration(filePath, durationFromMediaStore);

                                    string finalTitle = GetBestTitle(title, displayName, filePath);
                                    string? thumbnailPath = GenerateThumbnail(contentResolver, id, filePath);

                                    videoFiles.Add(new VideoInfo
                                    {
                                        FilePath = filePath,
                                        Title = finalTitle,
                                        DurationMilliseconds = duration,
                                        ThumbnailPath = thumbnailPath
                                    });

                                    System.Diagnostics.Debug.WriteLine(
                                        $"Added video: {finalTitle}, Duration: {TimeSpan.FromMilliseconds(duration):hh\\:mm\\:ss}, Path: {filePath}");
                                }
                                catch (Exception itemEx)
                                {
                                    System.Diagnostics.Debug.WriteLine(
                                        $"Error processing video item: {itemEx.Message}");
                                }
                            } while (cursor.MoveToNext());
                        }

                        cursor.Close();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MediaStore cursor is null.");
                    }
                }
            }
            catch (Exception queryEx)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error querying MediaStore: {queryEx.Message}\n{queryEx.StackTrace}");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Error", "Could not retrieve video list.", "OK");
                });
            }
        });
#else
        System.Diagnostics.Debug.WriteLine("Video discovery is only implemented for Android.");
        await Task.CompletedTask;
#endif

        return videoFiles;
    }

#if ANDROID
    private long GetAccurateDuration(string filePath, long mediaStoreDuration)
    {
        long duration = 0;
        MediaMetadataRetriever? retriever = null;

        try
        {
            retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(filePath);

            string? durationString = retriever.ExtractMetadata(MetadataKey.Duration);

            if (!string.IsNullOrEmpty(durationString) && long.TryParse(durationString, out long retrievedDuration))
            {
                duration = retrievedDuration;
                System.Diagnostics.Debug.WriteLine(
                    $"Duration from MediaMetadataRetriever: {duration}ms ({TimeSpan.FromMilliseconds(duration):hh\\:mm\\:ss}) for {Path.GetFileName(filePath)}");
            }

            if (duration <= 0 && mediaStoreDuration > 0)
            {
                if (mediaStoreDuration < 10000)
                {
                    duration = mediaStoreDuration * 1000;
                    System.Diagnostics.Debug.WriteLine(
                        $"Duration from MediaStore (converted to ms): {duration}ms for {Path.GetFileName(filePath)}");
                }
                else
                {
                    duration = mediaStoreDuration;
                    System.Diagnostics.Debug.WriteLine(
                        $"Duration from MediaStore: {duration}ms for {Path.GetFileName(filePath)}");
                }
            }

            if (duration > 0)
            {
                if (duration < 500)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Warning: Duration too short ({duration}ms), trying alternative method");
                    duration = EstimateDurationFromBitrate(retriever, filePath);
                }
                else if (duration > 86400000)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Warning: Duration too long ({duration}ms), trying alternative method");
                    duration = EstimateDurationFromBitrate(retriever, filePath);
                }
            }

            if (duration <= 0)
            {
                duration = EstimateDurationFromBitrate(retriever, filePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting duration for {filePath}: {ex.Message}");

            if (duration <= 0)
            {
                duration = EstimateDurationFromFileSize(filePath);
            }
        }
        finally
        {
            try
            {
                retriever?.Release();
                retriever?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error releasing MediaMetadataRetriever: {ex.Message}");
            }
        }

        return duration;
    }

    private long EstimateDurationFromBitrate(MediaMetadataRetriever retriever, string filePath)
    {
        try
        {
            string? bitrateString = retriever.ExtractMetadata(MetadataKey.Bitrate);

            if (!string.IsNullOrEmpty(bitrateString) && long.TryParse(bitrateString, out long bitrate) && bitrate > 0)
            {
                var fileInfo = new FileInfo(filePath);
                long fileSizeBytes = fileInfo.Length;

                long estimatedDuration = (fileSizeBytes * 8 * 1000) / bitrate;

                if (estimatedDuration > 0 && estimatedDuration < 86400000)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Estimated duration from bitrate ({bitrate} bps): {estimatedDuration}ms ({TimeSpan.FromMilliseconds(estimatedDuration):hh\\:mm\\:ss}) for {Path.GetFileName(filePath)}");
                    return estimatedDuration;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error estimating duration from bitrate: {ex.Message}");
        }

        return 0;
    }

    private long EstimateDurationFromFileSize(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            long fileSizeBytes = fileInfo.Length;

            long averageBitrate = 2_500_000;
            long estimatedDuration = (fileSizeBytes * 8 * 1000) / averageBitrate;

            if (estimatedDuration >= 1000 && estimatedDuration < 86400000)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Estimated duration from file size: {estimatedDuration}ms (~{TimeSpan.FromMilliseconds(estimatedDuration):hh\\:mm\\:ss}) for {Path.GetFileName(filePath)}");
                return estimatedDuration;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error estimating duration from file size: {ex.Message}");
        }

        return 0;
    }

    private string GetBestTitle(string title, string displayName, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(title) && title.Trim().Length > 0)
        {
            return title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(displayName) && displayName.Trim().Length > 0)
        {
            return Path.GetFileNameWithoutExtension(displayName.Trim());
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }

    [Obsolete("Obsolete")]
    private string? GenerateThumbnail(ContentResolver contentResolver, long videoId, string filePath)
    {
        Bitmap? thumbnailBitmap = null;
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                try
                {
                    var videoUri = ContentUris.WithAppendedId(MediaStore.Video.Media.ExternalContentUri, videoId);
                    thumbnailBitmap = contentResolver.LoadThumbnail(videoUri, new Android.Util.Size(320, 240), null);
                    System.Diagnostics.Debug.WriteLine($"Thumbnail loaded via ContentResolver for {filePath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load thumbnail via ContentResolver: {ex.Message}");
                }
            }

            if (thumbnailBitmap == null)
            {
                MediaMetadataRetriever? retriever = null;
                try
                {
                    retriever = new MediaMetadataRetriever();
                    retriever.SetDataSource(filePath);

                    long[] timePositions = { 1_000_000, 5_000_000, 10_000_000, 0 };
                    foreach (var timeUs in timePositions)
                    {
                        try
                        {
                            thumbnailBitmap =
                                retriever.GetFrameAtTime(timeUs, MediaMetadataRetriever.OptionClosestSync);
                            if (thumbnailBitmap != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Thumbnail extracted at {timeUs}µs for {filePath}");
                                break;
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in MediaMetadataRetriever: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        retriever?.Release();
                        retriever?.Dispose();
                    }
                    catch { }
                }
            }

            if (thumbnailBitmap != null)
            {
                string thumbnailFileName = $"thumb_{Path.GetFileNameWithoutExtension(filePath)}_{videoId}.jpg";
                string cachedThumbnailPath = Path.Combine(FileSystem.CacheDirectory, thumbnailFileName);

                if (File.Exists(cachedThumbnailPath))
                {
                    thumbnailBitmap.Recycle();
                    return cachedThumbnailPath;
                }

                using (var stream = new FileStream(cachedThumbnailPath, FileMode.Create))
                {
                    thumbnailBitmap.Compress(Bitmap.CompressFormat.Jpeg, 85, stream);
                }

                thumbnailBitmap.Recycle();
                return cachedThumbnailPath;
            }
        }
        catch (Exception thumbEx)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating thumbnail for {filePath}: {thumbEx.Message}");
        }

        return null;
    }
#endif
}
