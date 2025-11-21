namespace TheLighthouseWavesPlayerVideoApp.Interfaces;

public interface IPermissionService
{
    Task<bool> CheckAndRequestStoragePermissionAsync();
    Task<PermissionStatus> GetStoragePermissionStatusAsync();
    void OpenAppSettings();
}
