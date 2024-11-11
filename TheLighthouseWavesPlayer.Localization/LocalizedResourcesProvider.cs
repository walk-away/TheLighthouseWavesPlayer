using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using TheLighthouseWavesPlayer.Localization.Interfaces;

namespace TheLighthouseWavesPlayer.Localization;

public class LocalizedResourcesProvider : ObservableObject, ILocalizedResourcesProvider
{
    ResourceManager _resourceManager;

    CultureInfo _currentCulture;

    public static LocalizedResourcesProvider Instance
    {
        get;
        private set;
    }

    public string this[string key]
        => _resourceManager.GetString(key, _currentCulture)
           ?? key;

    public LocalizedResourcesProvider(ResourceManager resourceManager)
    {
        this._resourceManager = resourceManager;
        _currentCulture = CultureInfo.CurrentUICulture;
        Instance = this;
    }

    public void UpdateCulture(CultureInfo cultureInfo)
    {
        _currentCulture = cultureInfo;
        OnPropertyChanged("Item");
    }
}