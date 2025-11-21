namespace TheLighthouseWavesPlayerVideoApp.Models;

public class ThemeOption
{
    public string Name { get; set; } = string.Empty;
    public AppTheme Theme { get; set; }

    public override string? ToString() => Name;
}
