using TheLighthouseWavesPlayerVideoApp.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class PermissionService : IPermissionService
{
    private sealed class VideoPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
                {
                    return [("android.permission.READ_MEDIA_VIDEO", true)];
                }

                return [("android.permission.READ_EXTERNAL_STORAGE", true)];
            }
        }
    }

    public async Task<bool> CheckAndRequestStoragePermissionAsync()
    {
        try
        {
            var status = await GetStoragePermissionStatusAsync();

            if (status == PermissionStatus.Granted)
            {
                return true;
            }

            status = await RequestStoragePermissionAsync();

            if (status == PermissionStatus.Granted)
            {
                return true;
            }

            await ShowPermissionDeniedDialogAsync();

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking permissions: {ex.Message}");
            return false;
        }
    }

    public async Task<PermissionStatus> GetStoragePermissionStatusAsync()
    {
        return await Permissions.CheckStatusAsync<VideoPermission>();
    }

    private async Task<PermissionStatus> RequestStoragePermissionAsync()
    {
        return await Permissions.RequestAsync<VideoPermission>();
    }

    private async Task ShowPermissionDeniedDialogAsync()
    {
        if (MainThread.IsMainThread)
        {
            await ShowDialogInternal();
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(ShowDialogInternal);
        }
    }

    private async Task ShowDialogInternal()
    {
        bool openSettings = await Shell.Current.DisplayAlert(
            "Permission Required",
            "Video file access is required for this app to work. Please grant the permission in app settings.",
            "Open Settings",
            "Cancel"
        );

        if (openSettings)
        {
            OpenAppSettings();
        }
    }

    public void OpenAppSettings()
    {
        try
        {
            AppInfo.Current.ShowSettingsUI();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening settings: {ex.Message}");
        }
    }
}
