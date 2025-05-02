using TheLighthouseWavesPlayerVideoApp.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class ScreenWakeService : IScreenWakeService
{
    public void KeepScreenOn()
    {
        try
        {
            DeviceDisplay.Current.KeepScreenOn = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to keep screen on: {ex.Message}");
        }
    }

    public void AllowScreenSleep()
    {
        try
        {
            DeviceDisplay.Current.KeepScreenOn = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to allow screen sleep: {ex.Message}");
        }
    }
}