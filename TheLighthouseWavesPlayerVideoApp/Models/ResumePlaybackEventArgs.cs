namespace TheLighthouseWavesPlayerVideoApp.Models;

public class ResumePlaybackEventArgs : EventArgs
{
    public TimeSpan Position { get; }
    public ResumePlaybackEventArgs(TimeSpan position) => Position = position;
}
