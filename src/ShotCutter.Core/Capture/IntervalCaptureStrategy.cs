using ShotCutter.Core.Helpers;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.Core.Capture;

/// <summary>
/// Captures frames at a fixed interval (e.g. every N seconds).
/// Uses FFmpeg fps filter to extract frames across the entire video in a single pass.
/// </summary>
public sealed class IntervalCaptureStrategy : ICaptureStrategy
{
    private readonly IFFmpegService _ffmpeg;

    public IntervalCaptureStrategy(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public CaptureMode Mode => CaptureMode.Interval;

    public async Task<IReadOnlyList<ScreenshotResult>> ExecuteAsync(
        VideoInfo video,
        CaptureOptions options,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var ext = _ffmpeg.GetOutputExtension(options.Format);
        var outputPattern = Path.Combine(outputDirectory, $"frame_%06d{ext}");
        var fps = 1.0 / options.IntervalSeconds;

        var cmd = new FFmpegCommandBuilder()
            .Input(video.FilePath)
            .Fps(fps)
            .Scale(options.ScaleWidth, options.ScaleHeight)
            .Format(options.Format, options.Quality)
            .Output(outputPattern)
            .Build();

        var totalFrames = (int)Math.Ceiling(video.Duration.TotalSeconds / options.IntervalSeconds);
        var frameCount = 0;

        await _ffmpeg.ExecuteAsync(cmd, line =>
        {
            if (line.Contains("frame=") || line.Contains("frame "))
            {
                frameCount++;
                progress?.Report(totalFrames > 0 ? Math.Min(1.0, (double)frameCount / totalFrames) : 0);
            }
        }, ct);

        return CollectResults(outputDirectory, ext, options.IntervalSeconds);
    }

    private static IReadOnlyList<ScreenshotResult> CollectResults(string dir, string ext, double interval)
    {
        var files = Directory.GetFiles(dir, $"*{ext}")
            .OrderBy(f => f)
            .ToList();

        var results = new List<ScreenshotResult>();
        for (var i = 0; i < files.Count; i++)
        {
            var fi = new FileInfo(files[i]);
            results.Add(new ScreenshotResult
            {
                ImagePath = files[i],
                Timestamp = TimeSpan.FromSeconds(i * interval),
                Width = 0, // populated by caller if needed
                Height = 0,
                FileSizeBytes = fi.Length
            });
        }

        return results;
    }
}
