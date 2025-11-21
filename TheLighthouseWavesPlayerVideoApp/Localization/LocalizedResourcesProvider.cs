using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;

namespace TheLighthouseWavesPlayerVideoApp.Localization;

public class LocalizedResourcesProvider : ObservableObject, ILocalizedResourcesProvider
{
    private readonly ResourceManager _resourceManager;

    private CultureInfo? _currentCulture;

    public static LocalizedResourcesProvider Instance { get; private set; } = null!;

    public string this[string key]
        => _resourceManager.GetString(key, _currentCulture)
           ?? key;

    public LocalizedResourcesProvider(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
        _currentCulture = CultureInfo.CurrentUICulture;
        Instance = this;
    }

    public void UpdateCulture(CultureInfo? cultureInfo)
    {
        _currentCulture = cultureInfo;
        OnPropertyChanged("Item");
    }
}
