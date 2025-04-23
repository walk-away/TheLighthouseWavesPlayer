using CommunityToolkit.Maui.Views;

namespace TheLighthouseWavesPlayerVideoApp.Interfaces;

public interface IScreenshotService
{
    Task<string> CaptureScreenshotAsync(MediaElement mediaElement);
}