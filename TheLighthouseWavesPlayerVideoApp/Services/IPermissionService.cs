namespace TheLighthouseWavesPlayerVideoApp.Services;

public interface IPermissionService
{
    Task<bool> CheckAndRequestVideoPermissions();
}