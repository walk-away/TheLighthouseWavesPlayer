﻿using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Colors.Blue;
        public Color FalseColor { get; set; } = Colors.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueColor : FalseColor;
            }
            return FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Color color && color.Equals(TrueColor);
        }
    }
}