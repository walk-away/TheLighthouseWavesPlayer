namespace TheLighthouseWavesPlayerVideoApp.Models;

public class SeekEventArgs : EventArgs
{
    public TimeSpan Position { get; }
    public SeekEventArgs(TimeSpan position) => Position = position;
}
