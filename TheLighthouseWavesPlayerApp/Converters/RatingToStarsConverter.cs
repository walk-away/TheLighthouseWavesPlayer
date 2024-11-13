using System.Globalization;

namespace TheLighthouseWavesPlayerApp.Converters;

public class RatingToStarsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double rating || rating < 0 || rating > 5)
        {
            return string.Empty;
        }

        string fullStar = "\ue838";
        string halfStar = "\ue839";

        int fullStars = (int)rating;
        bool hasHalfStar = rating % 1 >= 0.5;

        return string.Concat(
            string.Join("", Enumerable.Repeat(fullStar, fullStars)),
            hasHalfStar ? halfStar : "");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string stars)
        {
            throw new ArgumentException("Expected value to be a string", nameof(value));
        }

        string fullStar = "\ue838";
        string halfStar = "\ue839";

        int fullStars = stars.Count(c => c.ToString() == fullStar);
        int halfStars = stars.Count(c => c.ToString() == halfStar);

        return fullStars + (halfStars > 0 ? 0.5 : 0);
    }
}