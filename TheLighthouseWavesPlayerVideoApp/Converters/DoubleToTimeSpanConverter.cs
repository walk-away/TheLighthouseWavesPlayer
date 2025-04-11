using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class DoubleToTimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }
        return TimeSpan.Zero;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return timeSpan.TotalSeconds;
        }
        return 0.0;
    }
}