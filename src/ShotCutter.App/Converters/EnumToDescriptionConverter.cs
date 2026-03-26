using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace ShotCutter.App.Converters;

/// <summary>
/// Converts an enum value to its [Description] attribute text.
/// Falls back to the enum name when no attribute is present.
/// </summary>
[ValueConversion(typeof(Enum), typeof(string))]
public sealed class EnumToDescriptionConverter : IValueConverter
{
    public static readonly EnumToDescriptionConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Enum e) return value?.ToString() ?? string.Empty;

        var member = e.GetType().GetMember(e.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? e.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
