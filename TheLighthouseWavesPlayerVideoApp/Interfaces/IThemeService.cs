namespace TheLighthouseWavesPlayerVideoApp.Interfaces;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    void SetTheme(AppTheme theme);
    void ApplyTheme();
}