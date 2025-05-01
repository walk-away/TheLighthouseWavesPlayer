using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Localization.Interfaces;

public interface ILocalizedResourcesProvider
{
    string this[string key]
    {
        get;
    }

    void UpdateCulture(CultureInfo cultureInfo);
}
