using ShotCutter.SmartAnalysis.Models;

namespace ShotCutter.SmartAnalysis;

/// <summary>
/// Analyses a video file and returns a list of detected scene segments.
/// </summary>
public interface ISceneAnalyzer
{
    /// <summary>
    /// Detects scene boundaries in <paramref name="videoPath"/> and returns ordered scene segments.
    /// </summary>
    /// <param name="videoPath">Absolute path to the video file.</param>
    /// <param name="sensitivity">
    ///   Detection sensitivity (0.05 = very sensitive, 0.95 = only detect major cuts).
    ///   Corresponds to FFmpeg's scene change threshold.
    /// </param>
    /// <param name="videoDuration">Total duration of the video (for calculating segment end times).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<SceneSegment>> AnalyzeAsync(
        string videoPath,
        double sensitivity = 0.3,
        TimeSpan? videoDuration = null,
        CancellationToken ct = default);
}
