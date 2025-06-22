using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters
{
    public class BoolToSubtitleTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue 
                ? Application.Current.RequestedTheme == AppTheme.Dark
                    ? Colors.White
                    : Colors.Black
                : Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}