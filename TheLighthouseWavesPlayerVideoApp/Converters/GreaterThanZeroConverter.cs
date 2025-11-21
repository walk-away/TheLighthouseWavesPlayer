using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class GreaterThanZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i > 0,
            long l => l > 0,
            double d => d > 0,
            _ => false
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
