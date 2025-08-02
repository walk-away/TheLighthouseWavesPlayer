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

public class ScreenshotService : IScreenshotService
{
    public async Task<string> CaptureScreenshotAsync(MediaElement mediaElement)
    {
        if (mediaElement == null)
            throw new ArgumentNullException(nameof(mediaElement));

#if ANDROID
        try
        {
            Bitmap? videoFrameBitmap = await TryCaptureVideoFrameAsync(mediaElement);

            if (videoFrameBitmap != null)
            {
                System.Diagnostics.Debug.WriteLine("Successfully captured video frame.");
                string fileName = $"VideoFrame_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                try
                {
                    string savedPath = await SaveBitmapToGalleryUsingMediaStoreAsync(videoFrameBitmap, fileName);
                    System.Diagnostics.Debug.WriteLine($"Video frame saved via MediaStore: {savedPath}");
                    return savedPath;
                }
                finally
                {
                    videoFrameBitmap.Recycle();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to capture video frame directly. Falling back to screen capture.");
                return await CaptureScreenFallbackAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot capture/save error: {ex}");
            throw;
        }
#else
        System.Diagnostics.Debug.WriteLine("Platform is not Android. Using standard screen capture.");
        return await CaptureScreenFallbackAsync();
#endif
    }

#if ANDROID

    private async Task<Bitmap?> TryCaptureVideoFrameAsync(MediaElement mediaElement)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.N)
        {
            System.Diagnostics.Debug.WriteLine("PixelCopy requires Android API 24+. Frame capture skipped.");
            return null;
        }

        Android.Views.View? platformView = mediaElement.Handler?.PlatformView as Android.Views.View;
        if (platformView == null)
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

        Bitmap? bitmap = null;
        try
        {
            bitmap = Bitmap.CreateBitmap(surfaceView.Width, surfaceView.Height, Bitmap.Config.Argb8888!);
            if (bitmap == null)
            {
                 System.Diagnostics.Debug.WriteLine("Failed to create Bitmap for PixelCopy.");
                 return null;
            }

            var listener = new PixelCopyListener();
            var tcs = new TaskCompletionSource<bool>();
            listener.TaskCompletionSource = tcs;
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    Handler? handler = surfaceView.Handler ?? platformView.Handler;
                    if (handler != null && Build.VERSION.SdkInt >= BuildVersionCodes.N)
                    {
                        PixelCopy.Request(surfaceView.Holder.Surface, bitmap, listener, handler);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No valid Handler found for PixelCopy or insufficient Android version.");
                        tcs.TrySetResult(false);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PixelCopy.Request failed: {ex.Message}");
                    tcs.TrySetResult(false);
                }
            });

            bool success = await tcs.Task;

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("PixelCopy successful.");
                return bitmap;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PixelCopy failed.");
                bitmap.Recycle();
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during PixelCopy attempt: {ex}");
            bitmap?.Recycle();
            return null;
        }
    }
    
    private SurfaceView? FindSurfaceView(Android.Views.View? parent)
    {
        if (parent == null)
            return null;

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
                if (found != null) return found;
            }
        }
        return null;
    }
    
    private class PixelCopyListener : Java.Lang.Object, PixelCopy.IOnPixelCopyFinishedListener
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
    
    private async Task<string> SaveBitmapToGalleryUsingMediaStoreAsync(Bitmap bitmap, string fileName)
    {
        Uri? imageUri = null;
        Stream? outputStream = null;
        ContentResolver? contentResolver = Application.Context.ContentResolver;

        if (contentResolver == null) 
            throw new InvalidOperationException("ContentResolver is null.");

        try
        {
            ContentValues values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                string? picturesDir = Environment.DirectoryPictures;
                if (!string.IsNullOrEmpty(picturesDir))
                {
                    values.Put(MediaStore.IMediaColumns.RelativePath, Path.Combine(picturesDir, "VideoPlayerScreenshots"));
                }
                values.Put(MediaStore.IMediaColumns.IsPending, 1);
                imageUri = contentResolver.Insert(MediaStore.Images.Media.ExternalContentUri ?? throw new InvalidOperationException("ExternalContentUri is null"), values);
            }
            else
            {
                var publicPicturesDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures);
                if (publicPicturesDir?.AbsolutePath != null)
                {
                    string directory = Path.Combine(publicPicturesDir.AbsolutePath, "VideoPlayerScreenshots");
                    if (!Directory.Exists(directory)) 
                        Directory.CreateDirectory(directory);
                    string filePath = Path.Combine(directory, fileName);
                    values.Put(MediaStore.IMediaColumns.Data, filePath);
                }
                imageUri = contentResolver.Insert(MediaStore.Images.Media.ExternalContentUri ?? throw new InvalidOperationException("ExternalContentUri is null"), values);
            }

            if (imageUri == null) 
                throw new IOException("Failed to create new MediaStore record for Bitmap.");

            outputStream = contentResolver.OpenOutputStream(imageUri);
            if (outputStream == null) 
                throw new IOException($"Failed to get output stream for Bitmap URI: {imageUri}");

            Bitmap.CompressFormat? jpegFormat = Bitmap.CompressFormat.Jpeg;
            if (jpegFormat == null)
                throw new InvalidOperationException("JPEG compression format is not available.");

            bool success = bitmap.Compress(jpegFormat, 95, outputStream);
            if (!success) 
                throw new IOException("Failed to compress Bitmap.");

            await outputStream.FlushAsync();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                values.Clear();
                values.Put(MediaStore.IMediaColumns.IsPending, 0);
                contentResolver.Update(imageUri, values, null, null);
            }

            return imageUri.ToString() ?? throw new InvalidOperationException("Failed to get URI string.");
        }
        catch(Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"Error saving Bitmap to MediaStore: {ex}");
             if (imageUri != null && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
             {
                 try { contentResolver.Delete(imageUri, null, null); } 
                 catch {}
             }
             throw;
        }
        finally
        {
            outputStream?.Close();
        }
    }
    
    private async Task<string> CaptureScreenFallbackAsync()
    {
        var screenshot = await Screenshot.CaptureAsync();
        if (screenshot == null)
        {
            System.Diagnostics.Debug.WriteLine("Fallback screen capture failed (Screenshot.CaptureAsync returned null).");
            throw new InvalidOperationException("Failed to capture screen.");
        }
        System.Diagnostics.Debug.WriteLine("Captured screen as fallback.");
        string fileName = $"Screen_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        return await SaveScreenshotResultToGalleryUsingMediaStoreAsync(screenshot, fileName);
    }
    
    private async Task<string> SaveScreenshotResultToGalleryUsingMediaStoreAsync(IScreenshotResult screenshot, string fileName)
    {
        Uri? imageUri = null;
        Stream? outputStream = null;
        Stream? inputStream = null;
        ContentResolver? contentResolver = Application.Context.ContentResolver;

        if (contentResolver == null) 
            throw new InvalidOperationException("ContentResolver is null.");

        try
        {
            ContentValues values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");
            
             if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                string? picturesDir = Environment.DirectoryPictures;
                if (!string.IsNullOrEmpty(picturesDir))
                {
                    values.Put(MediaStore.IMediaColumns.RelativePath, Path.Combine(picturesDir, "VideoPlayerScreenshots"));
                }
                values.Put(MediaStore.IMediaColumns.IsPending, 1);
                imageUri = contentResolver.Insert(MediaStore.Images.Media.ExternalContentUri ?? throw new InvalidOperationException("ExternalContentUri is null"), values);
            }
            else
            {
                var publicPicturesDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures);
                if (publicPicturesDir?.AbsolutePath != null)
                {
                    string directory = Path.Combine(publicPicturesDir.AbsolutePath, "VideoPlayerScreenshots");
                    if (!Directory.Exists(directory)) 
                        Directory.CreateDirectory(directory);
                    string filePath = Path.Combine(directory, fileName);
                    values.Put(MediaStore.IMediaColumns.Data, filePath);
                }
                imageUri = contentResolver.Insert(MediaStore.Images.Media.ExternalContentUri ?? throw new InvalidOperationException("ExternalContentUri is null"), values);
            }

            if (imageUri == null) 
                throw new IOException("Failed to create new MediaStore record for ScreenshotResult.");

            outputStream = contentResolver.OpenOutputStream(imageUri);
            if (outputStream == null) 
                throw new IOException($"Failed to get output stream for ScreenshotResult URI: {imageUri}");

            inputStream = await screenshot.OpenReadAsync();
            await inputStream.CopyToAsync(outputStream);
            await outputStream.FlushAsync();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                values.Clear();
                values.Put(MediaStore.IMediaColumns.IsPending, 0);
                contentResolver.Update(imageUri, values, null, null);
            }

            return imageUri.ToString() ?? throw new InvalidOperationException("Failed to get URI string.");
        }
        catch(Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"Error saving ScreenshotResult to MediaStore: {ex}");
             if (imageUri != null && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
             {
                 try { contentResolver.Delete(imageUri, null, null); } 
                 catch {}
             }
             throw;
        }
        finally
        {
            inputStream?.Close();
            outputStream?.Close();
        }
    }

#endif
}