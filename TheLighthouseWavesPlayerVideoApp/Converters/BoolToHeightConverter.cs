using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class BoolToHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? 600 : 300;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
