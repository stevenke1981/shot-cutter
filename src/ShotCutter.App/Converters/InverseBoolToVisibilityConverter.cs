using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShotCutter.App.Converters;

/// <summary>
/// Inverts a boolean before converting to Visibility.
/// true  →  Collapsed
/// false →  Visible
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public static readonly InverseBoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}
