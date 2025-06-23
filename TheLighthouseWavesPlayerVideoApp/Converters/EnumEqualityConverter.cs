using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class EnumEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        string parameterString = parameter.ToString();
        if (Enum.IsDefined(value.GetType(), value) == false)
            return false;

        object parameterValue = Enum.Parse(value.GetType(), parameterString);
        return value.Equals(parameterValue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}