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