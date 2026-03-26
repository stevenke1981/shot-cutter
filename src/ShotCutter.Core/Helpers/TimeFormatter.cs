using System.Globalization;

namespace ShotCutter.Core.Helpers;

public static class TimeFormatter
{
    /// <summary>
    /// Formats a TimeSpan as HH:mm:ss.fff for FFmpeg -ss parameter.
    /// </summary>
    public static string ToFFmpegTimestamp(TimeSpan time)
    {
        return time.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses a timestamp string (HH:mm:ss.fff or seconds.milliseconds) to TimeSpan.
    /// </summary>
    public static TimeSpan ParseTimestamp(string timestamp)
    {
        if (double.TryParse(timestamp, CultureInfo.InvariantCulture, out var totalSeconds))
        {
            return TimeSpan.FromSeconds(totalSeconds);
        }

        if (TimeSpan.TryParseExact(timestamp, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out var ts))
        {
            return ts;
        }

        if (TimeSpan.TryParse(timestamp, CultureInfo.InvariantCulture, out var ts2))
        {
            return ts2;
        }

        throw new FormatException($"Cannot parse timestamp: '{timestamp}'");
    }

    /// <summary>
    /// Generates a safe filename from a timestamp, e.g. "00h12m34s567ms".
    /// </summary>
    public static string ToFilenamePart(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}h{time.Minutes:D2}m{time.Seconds:D2}s{time.Milliseconds:D3}ms";
    }
}
