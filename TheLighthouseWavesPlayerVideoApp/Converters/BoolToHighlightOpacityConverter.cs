using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class BoolToHighlightOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? 1.0 : 0.5;
        }

        return 0.5;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
