using TheLighthouseWavesPlayerVideoApp.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp.Services;

public sealed class ThemeService : IThemeService
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
        if (Application.Current == null)
        {
            return;
        }

        Application.Current.Dispatcher.Dispatch(() =>
        {
            var theme = CurrentTheme;

            if (Application.Current.UserAppTheme != theme)
            {
                Application.Current.UserAppTheme = theme;
            }
        });
    }
}
