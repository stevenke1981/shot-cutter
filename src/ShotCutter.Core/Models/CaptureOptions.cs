namespace ShotCutter.Core.Models;

public enum OutputFormat
{
    Jpeg,
    Png,
    WebP
}

public sealed record CaptureOptions
{
    public CaptureMode Mode { get; init; } = CaptureMode.Interval;

    /// <summary>Interval in seconds for Interval mode.</summary>
    public double IntervalSeconds { get; init; } = 1.0;

    /// <summary>Specific time points for TimePoint mode.</summary>
    public IReadOnlyList<TimeSpan> TimePoints { get; init; } = [];

    /// <summary>Scene change threshold (0.0–1.0) for SceneChange mode.</summary>
    public double SceneChangeThreshold { get; init; } = 0.3;

    /// <summary>Whether to capture the first frame.</summary>
    public bool CaptureFirstFrame { get; init; } = true;

    /// <summary>Whether to capture the last frame.</summary>
    public bool CaptureLastFrame { get; init; } = true;

    // Output settings
    public OutputFormat Format { get; init; } = OutputFormat.Jpeg;
    public int Quality { get; init; } = 85;
    public int? ScaleWidth { get; init; }
    public int? ScaleHeight { get; init; }
}
