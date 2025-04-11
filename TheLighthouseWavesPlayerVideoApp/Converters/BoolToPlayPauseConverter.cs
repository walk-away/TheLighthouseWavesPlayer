using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class BoolToPlayPauseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? "⏸" : "▶";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}