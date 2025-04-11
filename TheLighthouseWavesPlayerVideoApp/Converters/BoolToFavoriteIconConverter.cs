using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class BoolToFavoriteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? "favorite_filled.png" : "favorite_outline.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}