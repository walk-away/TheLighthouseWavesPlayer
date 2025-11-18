using CommunityToolkit.Maui.Views;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using Path = System.IO.Path;

#if ANDROID
using Android.Content;
using Android.Provider;
using Android.OS;
using Android.Graphics;
using Android.Views;
using Application = Android.App.Application;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;
#endif

namespace TheLighthouseWavesPlayerVideoApp.Services;

public sealed class ScreenshotService : IScreenshotService
{
    public async Task<string> CaptureScreenshotAsync(MediaElement mediaElement)
    {
        ArgumentNullException.ThrowIfNull(mediaElement);

#if ANDROID
        Bitmap? videoFrameBitmap = await TryCaptureVideoFrameAsync(mediaElement);

        if (videoFrameBitmap != null)
        {
            System.Diagnostics.Debug.WriteLine("Successfully captured video frame.");
            string fileName = $"VideoFrame_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

            using (videoFrameBitmap)
            {
                string savedPath = await SaveBitmapToGalleryUsingMediaStoreAsync(videoFrameBitmap, fileName);
                System.Diagnostics.Debug.WriteLine($"Video frame saved via MediaStore: {savedPath}");
                return savedPath;
            }
        }

        System.Diagnostics.Debug.WriteLine("Failed to capture video frame directly. Falling back to screen capture.");
        return await CaptureScreenFallbackAsync();
#else
        System.Diagnostics.Debug.WriteLine("Platform is not Android. Using standard screen capture.");
        return await CaptureScreenFallbackAsync();
#endif
    }

#if ANDROID

    private static async Task<Bitmap?> TryCaptureVideoFrameAsync(MediaElement mediaElement)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.N)
        {
            System.Diagnostics.Debug.WriteLine("PixelCopy requires Android API 24+. Frame capture skipped.");
            return null;
        }

        if (mediaElement.Handler?.PlatformView is not Android.Views.View platformView)
        {
            System.Diagnostics.Debug.WriteLine("Could not get PlatformView from MediaElement Handler.");
            return null;
        }

        SurfaceView? surfaceView = FindSurfaceView(platformView);

        if (surfaceView?.Holder?.Surface == null ||
            !surfaceView.Holder.Surface.IsValid ||
            surfaceView.Width <= 0 ||
            surfaceView.Height <= 0)
        {
            System.Diagnostics.Debug.WriteLine("Suitable SurfaceView not found or not ready for PixelCopy.");
            return null;
        }

        return await CaptureFromSurfaceViewAsync(surfaceView, platformView);
    }

    private static async Task<Bitmap?> CaptureFromSurfaceViewAsync(SurfaceView surfaceView,
        Android.Views.View platformView)
    {
        Bitmap? bitmap = null;

        try
        {
            using var listener = new PixelCopyListener();
            var tcs = new TaskCompletionSource<bool>();
            listener.TaskCompletionSource = tcs;

            bitmap = Bitmap.CreateBitmap(surfaceView.Width, surfaceView.Height, Bitmap.Config.Argb8888!);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Handler? handler = surfaceView.Handler ?? platformView.Handler;
                if (handler != null && Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    PixelCopy.Request(surfaceView.Holder!.Surface!, bitmap, listener, handler);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "No valid Handler found for PixelCopy or insufficient Android version.");
                    tcs.TrySetResult(false);
                }
            });

            bool success = await tcs.Task;

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("PixelCopy successful.");
                var result = bitmap;
                bitmap = null;
                return result;
            }

            System.Diagnostics.Debug.WriteLine("PixelCopy failed.");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during PixelCopy attempt: {ex.Message}");
            return null;
        }
        finally
        {
            bitmap?.Recycle();
            bitmap?.Dispose();
        }
    }

    private static SurfaceView? FindSurfaceView(Android.Views.View? parent)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent is SurfaceView sv)
        {
            return sv;
        }

        if (parent is ViewGroup vg)
        {
            for (int i = 0; i < vg.ChildCount; i++)
            {
                var child = vg.GetChildAt(i);
                var found = FindSurfaceView(child);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private sealed class PixelCopyListener : Java.Lang.Object, PixelCopy.IOnPixelCopyFinishedListener
    {
        public TaskCompletionSource<bool>? TaskCompletionSource { get; set; }

        public void OnPixelCopyFinished(int copyResult)
        {
            System.Diagnostics.Debug.WriteLine($"PixelCopy finished with result: {copyResult}");
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                TaskCompletionSource?.TrySetResult(copyResult == (int)PixelCopyResult.Success);
            }
            else
            {
                TaskCompletionSource?.TrySetResult(false);
            }
        }
    }

    private static async Task<string> SaveBitmapToGalleryUsingMediaStoreAsync(Bitmap bitmap, string fileName)
    {
        ContentResolver? contentResolver = Application.Context.ContentResolver;
        if (contentResolver == null)
        {
            throw new InvalidOperationException("ContentResolver is null.");
        }

        Uri? imageUri = null;
        ContentValues? values = null;

        try
        {
            values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                imageUri = await SaveBitmapModernAsync(contentResolver, values, bitmap);
            }
            else
            {
                imageUri = await SaveBitmapLegacyAsync(contentResolver, values, bitmap, fileName);
            }

            return imageUri?.ToString() ?? throw new InvalidOperationException("Failed to get URI string.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving Bitmap to MediaStore: {ex}");
            await CleanupFailedImageAsync(contentResolver, imageUri);
            throw;
        }
        finally
        {
            values?.Dispose();
        }
    }

    private static async Task<Uri> SaveBitmapModernAsync(ContentResolver contentResolver, ContentValues values,
        Bitmap bitmap)
    {
        string? picturesDir = Environment.DirectoryPictures;
        if (!string.IsNullOrEmpty(picturesDir))
        {
            values.Put(MediaStore.IMediaColumns.RelativePath,
                Path.Combine(picturesDir, "VideoPlayerScreenshots"));
        }

        values.Put(MediaStore.IMediaColumns.IsPending, 1);

        Uri? imageUri = contentResolver.Insert(
            MediaStore.Images.Media.ExternalContentUri ??
            throw new InvalidOperationException("ExternalContentUri is null"), values);

        if (imageUri == null)
        {
            throw new IOException("Failed to create new MediaStore record for Bitmap.");
        }

        await using (Stream? outputStream = contentResolver.OpenOutputStream(imageUri))
        {
            if (outputStream == null)
            {
                throw new IOException($"Failed to get output stream for Bitmap URI: {imageUri}");
            }

            bool success = await bitmap.CompressAsync(
                Bitmap.CompressFormat.Jpeg ??
                throw new InvalidOperationException("JPEG compression format is not available."),
                95,
                outputStream);

            if (!success)
            {
                throw new IOException("Failed to compress Bitmap.");
            }

            await outputStream.FlushAsync();
        }

        values.Clear();
        values.Put(MediaStore.IMediaColumns.IsPending, 0);
        contentResolver.Update(imageUri, values, null, null);

        return imageUri;
    }

    private static async Task<Uri> SaveBitmapLegacyAsync(ContentResolver contentResolver, ContentValues values,
        Bitmap bitmap, string fileName)
    {
        var publicPicturesDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures);
        if (publicPicturesDir?.AbsolutePath != null)
        {
            string directory = Path.Combine(publicPicturesDir.AbsolutePath, "VideoPlayerScreenshots");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = Path.Combine(directory, fileName);
            values.Put(MediaStore.IMediaColumns.Data, filePath);
        }

        Uri? imageUri = contentResolver.Insert(
            MediaStore.Images.Media.ExternalContentUri ??
            throw new InvalidOperationException("ExternalContentUri is null"), values);

        if (imageUri == null)
        {
            throw new IOException("Failed to create new MediaStore record for Bitmap.");
        }

        await using (Stream? outputStream = contentResolver.OpenOutputStream(imageUri))
        {
            if (outputStream == null)
            {
                throw new IOException($"Failed to get output stream for Bitmap URI: {imageUri}");
            }

            bool success = await bitmap.CompressAsync(
                Bitmap.CompressFormat.Jpeg ??
                throw new InvalidOperationException("JPEG compression format is not available."),
                95,
                outputStream);

            if (!success)
            {
                throw new IOException("Failed to compress Bitmap.");
            }

            await outputStream.FlushAsync();
        }

        return imageUri;
    }

    private static async Task CleanupFailedImageAsync(ContentResolver? contentResolver, Uri? imageUri)
    {
        if (imageUri != null && Build.VERSION.SdkInt >= BuildVersionCodes.Q && contentResolver != null)
        {
            try
            {
                await Task.Run(() => contentResolver.Delete(imageUri, null, null));
            }
            catch (Exception cleanupEx)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup failed image: {cleanupEx.Message}");
            }
        }
    }

    private static async Task<string> CaptureScreenFallbackAsync()
    {
        var screenshot = await Screenshot.CaptureAsync();
        if (screenshot == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "Fallback screen capture failed (Screenshot.CaptureAsync returned null).");
            throw new InvalidOperationException("Failed to capture screen.");
        }

        System.Diagnostics.Debug.WriteLine("Captured screen as fallback.");
        string fileName = $"Screen_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        return await SaveScreenshotResultToGalleryUsingMediaStoreAsync(screenshot, fileName);
    }

    private static async Task<string> SaveScreenshotResultToGalleryUsingMediaStoreAsync(IScreenshotResult screenshot,
        string fileName)
    {
        ContentResolver? contentResolver = Application.Context.ContentResolver;
        if (contentResolver == null)
        {
            throw new InvalidOperationException("ContentResolver is null.");
        }

        Uri? imageUri = null;
        ContentValues? values = null;

        try
        {
            values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                imageUri = await SaveScreenshotModernAsync(contentResolver, values, screenshot);
            }
            else
            {
                imageUri = await SaveScreenshotLegacyAsync(contentResolver, values, screenshot, fileName);
            }

            return imageUri?.ToString() ?? throw new InvalidOperationException("Failed to get URI string.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving ScreenshotResult to MediaStore: {ex}");
            await CleanupFailedImageAsync(contentResolver, imageUri);
            throw;
        }
        finally
        {
            values?.Dispose();
        }
    }

    private static async Task<Uri> SaveScreenshotModernAsync(ContentResolver contentResolver, ContentValues values,
        IScreenshotResult screenshot)
    {
        string? picturesDir = Environment.DirectoryPictures;
        if (!string.IsNullOrEmpty(picturesDir))
        {
            values.Put(MediaStore.IMediaColumns.RelativePath,
                Path.Combine(picturesDir, "VideoPlayerScreenshots"));
        }

        values.Put(MediaStore.IMediaColumns.IsPending, 1);

        Uri? imageUri = contentResolver.Insert(
            MediaStore.Images.Media.ExternalContentUri ??
            throw new InvalidOperationException("ExternalContentUri is null"), values);

        if (imageUri == null)
        {
            throw new IOException("Failed to create new MediaStore record for ScreenshotResult.");
        }

        await using (Stream? outputStream = contentResolver.OpenOutputStream(imageUri))
        {
            if (outputStream == null)
            {
                throw new IOException($"Failed to get output stream for ScreenshotResult URI: {imageUri}");
            }

            await using (Stream inputStream = await screenshot.OpenReadAsync())
            {
                await inputStream.CopyToAsync(outputStream);
                await outputStream.FlushAsync();
            }
        }

        values.Clear();
        values.Put(MediaStore.IMediaColumns.IsPending, 0);
        contentResolver.Update(imageUri, values, null, null);

        return imageUri;
    }

    private static async Task<Uri> SaveScreenshotLegacyAsync(ContentResolver contentResolver, ContentValues values,
        IScreenshotResult screenshot, string fileName)
    {
        var publicPicturesDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures);
        if (publicPicturesDir?.AbsolutePath != null)
        {
            string directory = Path.Combine(publicPicturesDir.AbsolutePath, "VideoPlayerScreenshots");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = Path.Combine(directory, fileName);
            values.Put(MediaStore.IMediaColumns.Data, filePath);
        }

        Uri? imageUri = contentResolver.Insert(
            MediaStore.Images.Media.ExternalContentUri ??
            throw new InvalidOperationException("ExternalContentUri is null"), values);

        if (imageUri == null)
        {
            throw new IOException("Failed to create new MediaStore record for ScreenshotResult.");
        }

        await using (Stream? outputStream = contentResolver.OpenOutputStream(imageUri))
        {
            if (outputStream == null)
            {
                throw new IOException($"Failed to get output stream for ScreenshotResult URI: {imageUri}");
            }

            await using (Stream inputStream = await screenshot.OpenReadAsync())
            {
                await inputStream.CopyToAsync(outputStream);
                await outputStream.FlushAsync();
            }
        }

        return imageUri;
    }

#endif
}
