namespace TheLighthouseWavesPlayer.Core.Models;

public class AudioPosition
{
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
    public double PlayProgress { get; set; }
}