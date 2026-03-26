using ShotCutter.Core.Helpers;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.Core.Capture;

/// <summary>
/// Extracts all I-frames (keyframes) from the video.
/// Uses FFmpeg select filter to pick only I-frame picture types.
/// </summary>
public sealed class KeyFrameCaptureStrategy : ICaptureStrategy
{
    private readonly IFFmpegService _ffmpeg;

    public KeyFrameCaptureStrategy(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public CaptureMode Mode => CaptureMode.KeyFrame;

    public async Task<IReadOnlyList<ScreenshotResult>> ExecuteAsync(
        VideoInfo video,
        CaptureOptions options,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var ext = _ffmpeg.GetOutputExtension(options.Format);
        var outputPattern = Path.Combine(outputDirectory, $"keyframe_%06d{ext}");

        var cmd = new FFmpegCommandBuilder()
            .Input(video.FilePath)
            .SelectKeyFrames()
            .Scale(options.ScaleWidth, options.ScaleHeight)
            .Format(options.Format, options.Quality)
            .Output(outputPattern)
            .Build();

        var estimatedTotal = Math.Max(1, video.KeyFrameTimestamps.Count);
        var frameCount = 0;

        await _ffmpeg.ExecuteAsync(cmd, line =>
        {
            if (line.Contains("frame=") || line.Contains("frame "))
            {
                frameCount++;
                progress?.Report(Math.Min(1.0, (double)frameCount / estimatedTotal));
            }
        }, ct);

        progress?.Report(1.0);

        return CollectResults(outputDirectory, ext, video.KeyFrameTimestamps);
    }

    private static IReadOnlyList<ScreenshotResult> CollectResults(
        string dir, string ext, IReadOnlyList<TimeSpan> keyFrameTimestamps)
    {
        var files = Directory.GetFiles(dir, $"*{ext}")
            .OrderBy(f => f)
            .ToList();

        var results = new List<ScreenshotResult>();
        for (var i = 0; i < files.Count; i++)
        {
            var fi = new FileInfo(files[i]);
            var ts = i < keyFrameTimestamps.Count ? keyFrameTimestamps[i] : TimeSpan.Zero;

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
}
