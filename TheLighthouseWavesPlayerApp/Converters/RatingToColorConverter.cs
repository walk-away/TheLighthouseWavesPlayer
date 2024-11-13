using System.Globalization;

namespace TheLighthouseWavesPlayerApp.Converters;

public class RatingToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isBackground = parameter is string param
                            && param.ToLower() == "background";

        var hex = value switch
        {
            double r when r > 0 && r < 1.4 => isBackground ? "#E0F7FA" : "#ADD8E6",
            double r when r < 2.4 => isBackground ? "#F0C085" : "#CD7F32",
            double r when r < 3.5 => isBackground ? "#E5E5E5" : "#C0C0C0",
            double r when r <= 4.0 => isBackground ? "#FFF9D6" : "#FFD700",
            _ => "#EBEBEB",
        };

        return hex is null ? null : Color.FromArgb(hex);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color color)
        {
            throw new ArgumentException("Expected value to be a Color", nameof(value));
        }

        string hex = color.ToHex();

        return hex switch
        {
            "#ADD8E6" => 1.0,
            "#CD7F32" => 2.0,
            "#C0C0C0" => 3.0,
            "#FFD700" => 4.0,
            "#E0F7FA" => 1.0,
            "#F0C085" => 2.0,
            "#E5E5E5" => 3.0,
            "#FFF9D6" => 4.0,
            _ => throw new ArgumentException("Unexpected color value", nameof(value)),
        };
    }
}