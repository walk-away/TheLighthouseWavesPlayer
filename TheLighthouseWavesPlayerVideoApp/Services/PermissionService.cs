namespace TheLighthouseWavesPlayerVideoApp.Services;

public class PermissionService : IPermissionService
{
    public async Task<bool> CheckAndRequestVideoPermissions()
    {
        try
        {
            PermissionStatus status;
            
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                status = await Permissions.CheckStatusAsync<Permissions.Media>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Media>();
                }
            }
            else
            {
                status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.StorageRead>();
                }
            }
            
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Denied", 
                    "Cannot access videos without storage permission", "OK");
            }
            
            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Permission Error", 
                $"Error requesting permissions: {ex.Message}", "OK");
            return false;
        }
    }
}