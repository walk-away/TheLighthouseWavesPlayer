namespace TheLighthouseWavesPlayerVideoApp.Models;

public class ThemeOption
{
    public string Name { get; set; }
    public AppTheme Theme { get; set; }

    public override string ToString() => Name;
}