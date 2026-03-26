using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShotCutter.App.Converters;

/// <summary>
/// Converts an integer count to Visibility.
/// count == 0  →  Visible   (shows the "empty state" hint)
/// count  > 0  →  Collapsed (hides the hint)
/// </summary>
[ValueConversion(typeof(int), typeof(Visibility))]
public sealed class CountToVisibilityConverter : IValueConverter
{
    public static readonly CountToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int count = value is int i ? i : 0;
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
