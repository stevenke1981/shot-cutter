using System.Globalization;
using System.Windows.Data;

namespace ShotCutter.App.Converters;

/// <summary>
/// Converts a byte count (long) to a human-readable file-size string.
/// e.g.  1536 → "1.5 KB",  2097152 → "2.0 MB"
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public sealed class FileSizeConverter : IValueConverter
{
    public static readonly FileSizeConverter Instance = new();

    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double size = value switch
        {
            long l  => l,
            int  i  => i,
            ulong u => u,
            _       => 0
        };

        int unit = 0;
        while (size >= 1024 && unit < Units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{size:0} {Units[unit]}"
            : $"{size:0.0} {Units[unit]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
