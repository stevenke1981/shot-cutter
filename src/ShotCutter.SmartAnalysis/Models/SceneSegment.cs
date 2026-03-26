namespace ShotCutter.SmartAnalysis.Models;

/// <summary>
/// Represents a detected scene segment within a video.
/// </summary>
public sealed record SceneSegment
{
    /// <summary>Start timestamp of this segment.</summary>
    public TimeSpan StartTime { get; init; }

    /// <summary>End timestamp of this segment (equals start of next segment, or video end).</summary>
    public TimeSpan EndTime { get; init; }

    /// <summary>
    /// Representative timestamp for capture — defaults to the midpoint of the segment.
    /// </summary>
    public TimeSpan RepresentativeTime => StartTime + TimeSpan.FromSeconds(
        (EndTime - StartTime).TotalSeconds / 2.0);

    /// <summary>
    /// Scene change score (0.0–1.0) measured at the start boundary.
    /// Higher values indicate a bigger visual difference from the previous scene.
    /// </summary>
    public double ChangeScore { get; init; }

    /// <summary>Segment duration.</summary>
    public TimeSpan Duration => EndTime - StartTime;

    public override string ToString() =>
        $"[{StartTime:hh\\:mm\\:ss\\.fff} → {EndTime:hh\\:mm\\:ss\\.fff}] score={ChangeScore:0.00}";
}
