using CommunityToolkit.Maui.Views;
using TheLighthouseWavesPlayerVideoApp.Interfaces;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using Path = System.IO.Path;

#if ANDROID
using Android.Content;
using Android.Provider;
using Android.OS;
using Android.Graphics;
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
            if (Build.VERSION.SdkInt <= BuildVersionCodes.P)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("Requesting StorageWrite permission for Android 9 or older.");
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                    if (status != PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("StorageWrite permission denied (Android 9 or older).");
                        throw new UnauthorizedAccessException("Storage permission is required for older Android versions to save screenshots to gallery.");
                    }
                }
            }

            var screenshot = await Screenshot.CaptureAsync();
            if (screenshot == null)
            {
                System.Diagnostics.Debug.WriteLine("Screenshot.CaptureAsync returned null.");
                throw new InvalidOperationException("Failed to capture screenshot.");
            }

            System.Diagnostics.Debug.WriteLine("Screenshot captured successfully.");

            string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            string savedPath = await SaveScreenshotToGalleryUsingMediaStoreAsync(screenshot, fileName);

            System.Diagnostics.Debug.WriteLine($"Screenshot saved via MediaStore: {savedPath}");
            return savedPath;

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot capture/save error: {ex}");
            throw;
        }
#else
        System.Diagnostics.Debug.WriteLine("Screenshot capture is only implemented for Android in this service.");
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("Screenshot capture is only implemented for Android.");
#endif
    }

#if ANDROID
    private async Task<string> SaveScreenshotToGalleryUsingMediaStoreAsync(IScreenshotResult screenshot, string fileName)
    {
        Uri? imageUri = null;
        Stream? outputStream = null;
        Stream? inputStream = null;
        ContentResolver? contentResolver = Application.Context.ContentResolver; // Get resolver once

        if (contentResolver == null)
        {
             throw new InvalidOperationException("ContentResolver is null.");
        }

        try
        {
            ContentValues values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                values.Put(MediaStore.IMediaColumns.RelativePath, Path.Combine(Environment.DirectoryPictures, "VideoPlayerScreenshots"));
                values.Put(MediaStore.IMediaColumns.IsPending, 1);
                imageUri = contentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, values);
            }
            else
            {
                string directory = Path.Combine(
                    Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).AbsolutePath,
                    "VideoPlayerScreenshots");
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string filePath = Path.Combine(directory, fileName);
                values.Put(MediaStore.IMediaColumns.Data, filePath);
                imageUri = contentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, values);
            }


            if (imageUri == null)
            {
                throw new IOException("Failed to create new MediaStore record.");
            }
            System.Diagnostics.Debug.WriteLine($"MediaStore URI created/found: {imageUri}");

            outputStream = contentResolver.OpenOutputStream(imageUri);
            if (outputStream == null)
            {
                throw new IOException($"Failed to get output stream for URI: {imageUri}");
            }

            inputStream = await screenshot.OpenReadAsync();
            await inputStream.CopyToAsync(outputStream);
            await outputStream.FlushAsync();
            System.Diagnostics.Debug.WriteLine($"Screenshot data copied to MediaStore URI.");
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                values.Clear();
                values.Put(MediaStore.IMediaColumns.IsPending, 0);
                var updatedRows = contentResolver.Update(imageUri, values, null, null);
                System.Diagnostics.Debug.WriteLine($"MediaStore entry updated (IsPending=0): {updatedRows} rows affected.");
            }

            return imageUri.ToString();
        }
        catch(Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"Error saving to MediaStore: {ex}");
             if (imageUri != null && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
             {
                 try
                 {
                     contentResolver.Delete(imageUri, null, null);
                     System.Diagnostics.Debug.WriteLine($"Attempted to delete pending MediaStore entry: {imageUri}");
                 }
                 catch(Exception deleteEx)
                 {
                     System.Diagnostics.Debug.WriteLine($"Error deleting pending MediaStore entry: {deleteEx.Message}");
                 }
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