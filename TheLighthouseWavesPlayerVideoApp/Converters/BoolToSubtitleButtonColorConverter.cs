using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class BoolToSubtitleButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            return isEnabled ? Colors.Teal : Colors.Gray;
        }

        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
