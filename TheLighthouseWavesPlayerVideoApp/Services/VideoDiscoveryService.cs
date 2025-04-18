using TheLighthouseWavesPlayerVideoApp.Interfaces;
using TheLighthouseWavesPlayerVideoApp.Models;


#if ANDROID
using Android.Database;
using Android.Provider;
using Uri = Android.Net.Uri;
#endif
// No need for the extra #if ANDROID using block at the top if the main logic is guarded

namespace TheLighthouseWavesPlayerVideoApp.Services; // Ensure this namespace is correct

public class VideoDiscoveryService : IVideoDiscoveryService // Ensure class name matches registration if using DI
{
    public async Task<bool> RequestPermissionsAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (status == PermissionStatus.Granted)
            return true;

        if (Permissions.ShouldShowRationale<Permissions.StorageRead>())
        {
            // Prompt the user with additional information as to why the permission is needed
            await Shell.Current.DisplayAlert("Permission Needed", "Storage permission is required to find video files.", "OK");
        }

        status = await Permissions.RequestAsync<Permissions.StorageRead>();

        return status == PermissionStatus.Granted;
    }

    // Changed return type to Task<List<VideoInfo>> to match common practice,
    // but you can change it back to Task<IList<VideoInfo>> if needed.
    public async Task<IList<VideoInfo>> DiscoverVideosAsync()
    {
        if (!await RequestPermissionsAsync())
        {
            await Shell.Current.DisplayAlert("Permission Denied", "Cannot access videos without storage permission.", "OK");
            return new List<VideoInfo>(); // Return empty list if no permission
        }

        var videoFiles = new List<VideoInfo>();

        // Wrap the entire Android-specific section
#if ANDROID
        await Task.Run(() => // Keep Task.Run for background execution
        {
            try
            {
                var context = Platform.CurrentActivity;
                var contentResolver = context.ContentResolver;
            
                Uri? uri = MediaStore.Video.Media.ExternalContentUri;

                // Define the columns to retrieve (same as your original)
                string[] projection = {
                    MediaStore.Video.Media.InterfaceConsts.Id,
                    MediaStore.Video.Media.InterfaceConsts.Data, // File path
                    MediaStore.Video.Media.InterfaceConsts.Title,
                    MediaStore.Video.Media.InterfaceConsts.Duration
                };

                // Query the MediaStore for videos (same sort order as your original)
                ICursor? cursor = contentResolver.Query(
                    uri,
                    projection,
                    null, // Selection (WHERE clause), null for all videos
                    null, // Selection arguments
                    MediaStore.Video.Media.InterfaceConsts.DateAdded + " DESC" // Sort order
                );

                if (cursor != null)
                {
                    // Get column indices once before the loop for efficiency
                    int dataColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Data);
                    int titleColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Title);
                    int durationColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Duration);
                    // Add ID column index if you plan to use the ID
                    // int idColumn = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.Id);

                    if (cursor.MoveToFirst()) // Use MoveToFirst with do-while
                    {
                        do
                        {
                            try
                            {
                                // Check for invalid column indices (though GetColumnIndex should return -1 if not found)
                                if (dataColumn == -1 || titleColumn == -1 || durationColumn == -1)
                                {
                                     System.Diagnostics.Debug.WriteLine("Error: Column index not found.");
                                     continue; // Skip this item
                                }

                                string filePath = cursor.GetString(dataColumn) ?? string.Empty; // Use null-coalescing
                                string title = cursor.GetString(titleColumn) ?? string.Empty;
                                long duration = cursor.GetLong(durationColumn);

                                // Keep your File.Exists check and fallback title logic
                                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                                {
                                    videoFiles.Add(new VideoInfo
                                    {
                                        FilePath = filePath,
                                        Title = string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(filePath) : title,
                                        DurationMilliseconds = duration
                                        // Assign ID if needed: Id = cursor.GetLong(idColumn)
                                    });
                                }
                                else if (!string.IsNullOrEmpty(filePath))
                                {
                                     System.Diagnostics.Debug.WriteLine($"File not found or path empty: {filePath}");
                                }

                            }
                            catch (Exception itemEx)
                            {
                                // Log individual item error (same as your original)
                                System.Diagnostics.Debug.WriteLine($"Error processing video item: {itemEx.Message}");
                                // Continue processing other items
                            }
                        } while (cursor.MoveToNext());
                    }
                    cursor.Close(); // Ensure cursor is closed
                } // end if cursor != null
            }
            catch (Exception queryEx)
            {
                // Log query error (same as your original)
                System.Diagnostics.Debug.WriteLine($"Error querying MediaStore: {queryEx.Message}");
                // Show error on UI thread (same as your original)
                MainThread.BeginInvokeOnMainThread(async () => {
                    await Shell.Current.DisplayAlert("Error", "Could not retrieve video list.", "OK");
                });
            }
        }); // End Task.Run
#else
        // Handle non-Android platforms if necessary (return empty list or throw exception)
        System.Diagnostics.Debug.WriteLine("Video discovery is only implemented for Android.");
        await Task.CompletedTask; // To satisfy async method signature if returning directly
        return new List<VideoInfo>();
#endif

        return videoFiles;
    }
}