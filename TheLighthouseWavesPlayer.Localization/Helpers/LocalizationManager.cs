using System.Globalization;
using Microsoft.Maui.Storage;
using TheLighthouseWavesPlayer.Localization.Interfaces;

namespace TheLighthouseWavesPlayer.Localization.Helpers;

public class LocalizationManager : ILocalizationManager
{
    readonly ILocalizedResourcesProvider _resourceProvider;

    private CultureInfo _currentCulture;

    public LocalizationManager(ILocalizedResourcesProvider resoureProvider)
    {
        _resourceProvider = resoureProvider;
    }

    public void RestorePreviousCulture(CultureInfo defaultCulture = null)
        => SetCulture(GetUserCulture(defaultCulture));

    public CultureInfo GetUserCulture(CultureInfo defaultCulture = null)
    {
        if (_currentCulture is null)
        {
            var culture = Preferences.Default.Get("UserCulture", string.Empty);
            if (string.IsNullOrEmpty(culture))
            {
                _currentCulture = defaultCulture ?? CultureInfo.CurrentCulture;
            }
            else
            {
                _currentCulture = new CultureInfo(culture);
            }
        }
        return _currentCulture;
    }

    public void UpdateUserCulture(CultureInfo cultureInfo)
    {
        Preferences.Default.Set("UserCulture", cultureInfo.Name);
        SetCulture(cultureInfo);
        
        Application.Current.Dispatcher.Dispatch(() =>
        {
            if (Application.Current.MainPage is Shell shell)
            {
                UpdateShellLabels(shell);
            }
        });
    }

    private void UpdateShellLabels(Shell shell)
    {
        var provider = LocalizedResourcesProvider.Instance;

        shell.Title = provider["Shell_Title"];

        foreach (var item in shell.Items)
        {
            if (item is TabBar tabBar)
            {
                foreach (var tab in tabBar.Items)
                {
                    if (tab is Tab tabItem)
                    {
                        switch (tabItem.Title)
                        {
                            case "Library":
                            case "Библиотека":
                                tabItem.Title = provider["Shell_Library"];
                                break;
                            case "Favorites":
                            case "Избранное":
                                tabItem.Title = provider["Shell_Favorites"];
                                break;
                            case "Settings":
                            case "Настройки":
                                tabItem.Title = provider["Shell_Settings"];
                                break;
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
        Application.Current.Dispatcher.Dispatch(() =>
        {
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        });
        _resourceProvider.UpdateCulture(cultureInfo);
    }
}