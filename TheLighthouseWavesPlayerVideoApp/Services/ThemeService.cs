using TheLighthouseWavesPlayerVideoApp.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public class ThemeService : IThemeService
{
    private const string ThemePreferenceKey = "AppTheme";

    public AppTheme CurrentTheme
    {
        get
        {
            var themeName = Preferences.Default.Get(ThemePreferenceKey, AppTheme.Unspecified.ToString());
            if (Enum.TryParse<AppTheme>(themeName, out var theme))
            {
                return theme;
            }

            return AppTheme.Unspecified;
        }
    }

    public void SetTheme(AppTheme theme)
    {
        Preferences.Default.Set(ThemePreferenceKey, theme.ToString());
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            switch (CurrentTheme)
            {
                case AppTheme.Light:
                    Application.Current.UserAppTheme = AppTheme.Light;
                    break;
                case AppTheme.Dark:
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    break;
                case AppTheme.Unspecified:
                    Application.Current.UserAppTheme = AppTheme.Unspecified;
                    break;
            }
        });
    }
}