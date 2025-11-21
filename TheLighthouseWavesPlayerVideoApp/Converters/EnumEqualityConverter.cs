using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class EnumEqualityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        var valueType = value.GetType();
        var parameterString = parameter.ToString()!;
        if (!Enum.IsDefined(valueType, value))
        {
            return false;
        }

        var parameterValue = Enum.Parse(valueType, parameterString);
        return value.Equals(parameterValue);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
