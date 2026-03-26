using System.Text.RegularExpressions;
using ShotCutter.Core.Services;
using ShotCutter.SmartAnalysis.Models;

namespace ShotCutter.SmartAnalysis;

/// <summary>
/// Detects scene boundaries using FFmpeg's built-in scene score filter.
/// Runs <c>ffmpeg -vf "select='gte(scene,0)',metadata=print:file=-"</c> and parses
/// the per-frame <c>lavfi.scene_score</c> metadata to find frames that exceed the
/// configured sensitivity threshold.
/// </summary>
public sealed class HistogramSceneAnalyzer : ISceneAnalyzer
{
    private static readonly Regex PtsTimeRegex =
        new(@"pts_time:([\d.]+)", RegexOptions.Compiled);

    private static readonly Regex SceneScoreRegex =
        new(@"lavfi\.scene_score=([\d.]+(?:e[+-]?\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IFFmpegService _ffmpeg;

    public HistogramSceneAnalyzer(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SceneSegment>> AnalyzeAsync(
        string videoPath,
        double sensitivity = 0.3,
        TimeSpan? videoDuration = null,
        CancellationToken ct = default)
    {
        // Scale down to speed up analysis (quarter resolution), then extract scene scores.
        // We request scores for ALL frames (gte(scene,0)) so we can filter in code.
        var args = $"-i \"{videoPath}\" " +
                   $"-vf \"scale=iw/4:ih/4,select='gte(scene,0)',metadata=print:file=-\" " +
                   $"-vsync vfr -f null -";

        var frameMeta = new List<(double PtsTime, double Score)>();

        var result = await _ffmpeg.ExecuteAsync(args, ct: ct);

        // The metadata is written to stdout (file=-) while ffmpeg progress goes to stderr.
        // Parse both outputs for the frame lines.
        var outputToSearch = result.StandardOutput + "\n" + result.StandardError;

        ParseFrameMetadata(outputToSearch, frameMeta);

        if (frameMeta.Count == 0)
            return [];

        return BuildSegments(frameMeta, sensitivity, videoDuration);
    }

    // -------------------------------------------------------------------------

    private static void ParseFrameMetadata(string text, List<(double, double)> output)
    {
        // The metadata output format (one block per frame):
        //   frame:N   pts:P   pts_time:T
        //   lavfi.scene_score=S
        //
        // We walk line-by-line and pair pts_time lines with the following scene_score line.

        double? currentPtsTime = null;

        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var lineStr = line.ToString();

            var ptsMatch = PtsTimeRegex.Match(lineStr);
            if (ptsMatch.Success)
            {
                if (double.TryParse(ptsMatch.Groups[1].Value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var t))
                {
                    currentPtsTime = t;
                }
                continue;
            }

            var scoreMatch = SceneScoreRegex.Match(lineStr);
            if (scoreMatch.Success && currentPtsTime.HasValue)
            {
                if (double.TryParse(scoreMatch.Groups[1].Value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var score))
                {
                    output.Add((currentPtsTime.Value, score));
                }
                currentPtsTime = null;
            }
        }
    }

    private static IReadOnlyList<SceneSegment> BuildSegments(
        List<(double PtsTime, double Score)> frames,
        double threshold,
        TimeSpan? videoDuration)
    {
        // Find frames where scene_score >= threshold — these are the cut points.
        var cutTimes = frames
            .Where(f => f.Score >= threshold)
            .Select(f => f.PtsTime)
            .OrderBy(t => t)
            .ToList();

        // Build segments: first segment always starts at 0.
        var startTimes = new List<double> { 0.0 };
        startTimes.AddRange(cutTimes);

        var segments = new List<SceneSegment>(startTimes.Count);
        var maxTime = videoDuration?.TotalSeconds
                      ?? frames.Max(f => f.PtsTime);

        for (int i = 0; i < startTimes.Count; i++)
        {
            var start = startTimes[i];
            var end = i + 1 < startTimes.Count ? startTimes[i + 1] : maxTime;

            // Find the score at the cut boundary.
            var boundaryScore = i == 0 ? 0.0
                : frames
                    .Where(f => Math.Abs(f.PtsTime - start) < 1.0)
                    .Select(f => f.Score)
                    .DefaultIfEmpty(0.0)
                    .Max();

            segments.Add(new SceneSegment
            {
                StartTime = TimeSpan.FromSeconds(start),
                EndTime = TimeSpan.FromSeconds(end),
                ChangeScore = boundaryScore
            });
        }

        return segments.AsReadOnly();
    }
}
