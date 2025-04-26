using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters
{
    public class VolumeToIconConverter : IValueConverter
    {
        public string MutedIcon { get; set; } = "volumex.svg";
        public string UnmutedIcon { get; set; } = "volume2.svg";
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double volume)
            {
                return volume <= 0 ? MutedIcon : UnmutedIcon;
            }
            
            return UnmutedIcon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}