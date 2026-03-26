using System.Globalization;
using System.Text.RegularExpressions;
using ShotCutter.Core.Helpers;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.Core.Capture;

/// <summary>
/// Captures frames when the scene changes significantly.
/// Uses FFmpeg's scene detection filter and parses showinfo output for timestamps.
/// </summary>
public sealed partial class SceneChangeCaptureStrategy : ICaptureStrategy
{
    private readonly IFFmpegService _ffmpeg;

    public SceneChangeCaptureStrategy(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public CaptureMode Mode => CaptureMode.SceneChange;

    public async Task<IReadOnlyList<ScreenshotResult>> ExecuteAsync(
        VideoInfo video,
        CaptureOptions options,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var ext = _ffmpeg.GetOutputExtension(options.Format);
        var outputPattern = Path.Combine(outputDirectory, $"scene_%06d{ext}");

        var cmd = new FFmpegCommandBuilder()
            .Input(video.FilePath)
            .SelectSceneChange(options.SceneChangeThreshold)
            .Scale(options.ScaleWidth, options.ScaleHeight)
            .Format(options.Format, options.Quality)
            .Output(outputPattern)
            .Build();

        var timestamps = new List<double>();

        await _ffmpeg.ExecuteAsync(cmd, line =>
        {
            // Parse showinfo output for pts_time
            var match = PtsTimeRegex().Match(line);
            if (match.Success && double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var pts))
            {
                timestamps.Add(pts);
            }

            // Report progress based on time
            if (video.Duration.TotalSeconds > 0 && timestamps.Count > 0)
            {
                progress?.Report(Math.Min(1.0, timestamps[^1] / video.Duration.TotalSeconds));
            }
        }, ct);

        progress?.Report(1.0);

        return CollectResults(outputDirectory, ext, timestamps);
    }

    private static IReadOnlyList<ScreenshotResult> CollectResults(string dir, string ext, List<double> timestamps)
    {
        var files = Directory.GetFiles(dir, $"*{ext}")
            .OrderBy(f => f)
            .ToList();

        var results = new List<ScreenshotResult>();
        for (var i = 0; i < files.Count; i++)
        {
            var fi = new FileInfo(files[i]);
            var ts = i < timestamps.Count ? TimeSpan.FromSeconds(timestamps[i]) : TimeSpan.Zero;

            results.Add(new ScreenshotResult
            {
                ImagePath = files[i],
                Timestamp = ts,
                Width = 0,
                Height = 0,
                FileSizeBytes = fi.Length
            });
        }

        return results;
    }

    [GeneratedRegex(@"pts_time:([\d.]+)")]
    private static partial Regex PtsTimeRegex();
}
