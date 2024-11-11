using System.Globalization;

namespace TheLighthouseWavesPlayer.Localization.Interfaces;

public interface ILocalizedResourcesProvider
{
    string this[string key]
    {
        get;
    }

    void UpdateCulture(CultureInfo cultureInfo);
}
