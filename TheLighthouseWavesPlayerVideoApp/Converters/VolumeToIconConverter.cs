using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters;

public class VolumeToIconConverter : IValueConverter
{
    public string MutedIcon { get; set; } = "volumex.svg";
    public string UnmutedIcon { get; set; } = "volume2.svg";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is double d && d <= 0) ? MutedIcon : UnmutedIcon;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}