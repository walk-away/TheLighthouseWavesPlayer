using System.Globalization;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;
using Preferences = Microsoft.Maui.Storage.Preferences;

namespace TheLighthouseWavesPlayerVideoApp.Localization.Helpers;

public class LocalizationManager : ILocalizationManager
{
    private readonly ILocalizedResourcesProvider _resourceProvider;
    private CultureInfo _currentCulture;

    public LocalizationManager(ILocalizedResourcesProvider resourceProvider)
    {
        _resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
        _currentCulture = CultureInfo.CurrentCulture;
    }

    public void RestorePreviousCulture(CultureInfo? defaultCulture = null)
    {
        var culture = GetUserCulture(defaultCulture) ?? CultureInfo.CurrentCulture;
        SetCulture(culture);
    }

    public CultureInfo GetUserCulture(CultureInfo? defaultCulture = null)
    {
        var name = Preferences.Default.Get("UserCulture", defaultCulture?.Name ?? _currentCulture.Name);
        try
        {
            return new CultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return _currentCulture;
        }
    }

    public void UpdateUserCulture(CultureInfo cultureInfo)
    {
        if (cultureInfo == null)
        {
            throw new ArgumentNullException(nameof(cultureInfo));
        }

        Preferences.Default.Set("UserCulture", cultureInfo.Name);
        SetCulture(cultureInfo);

        Application.Current?.Dispatcher.Dispatch(() =>
        {
            if (Application.Current.MainPage is Shell shell)
            {
                UpdateShellLabels(shell);
            }
        });
    }

    private void UpdateShellLabels(Shell? shell)
    {
        if (shell == null)
        {
            return;
        }

        var provider = _resourceProvider;
        if (provider == null)
        {
            return;
        }

        shell.Title = provider["Shell_Title"] ?? shell.Title;

        foreach (var item in shell.Items)
        {
            if (item is TabBar tabBar)
            {
                foreach (var tab in tabBar.Items)
                {
                    if (tab is Tab tabItem)
                    {
                        string current = tabItem.Title;
                        string? newTitle = current switch
                        {
                            "Library" or "Библиотека" => provider["Shell_Library"],
                            "Favorites" or "Избранное" => provider["Shell_Favorites"],
                            "Playlists" or "Плейлисты" => provider["Shell_Playlists"],
                            "Settings" or "Настройки" => provider["Shell_Settings"],
                            _ => null
                        };
                        if (!string.IsNullOrEmpty(newTitle))
                        {
                            tabItem.Title = newTitle;
                        }

                        if (tabItem.Items.Count > 0 && tabItem.Items[0] is ShellContent content)
                        {
                            content.Title = tabItem.Title;
                        }
                    }
                }
            }
        }
    }

    private void SetCulture(CultureInfo cultureInfo)
    {
        _currentCulture = cultureInfo;
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        });
        _resourceProvider.UpdateCulture(cultureInfo);
    }
}
