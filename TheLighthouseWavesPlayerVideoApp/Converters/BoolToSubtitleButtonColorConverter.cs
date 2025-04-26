using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class BoolToSubtitleButtonColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            return isEnabled ? Color.FromArgb("#008080") : Color.FromArgb("#808080");
        }
        
        return Color.FromArgb("#808080");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}