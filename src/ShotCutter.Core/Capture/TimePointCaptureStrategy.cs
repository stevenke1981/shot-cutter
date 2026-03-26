using ShotCutter.Core.Helpers;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.Core.Capture;

/// <summary>
/// Captures frames at user-specified time points.
/// Each time point results in a separate FFmpeg call with -ss for fast seeking.
/// </summary>
public sealed class TimePointCaptureStrategy : ICaptureStrategy
{
    private readonly IFFmpegService _ffmpeg;

    public TimePointCaptureStrategy(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public CaptureMode Mode => CaptureMode.TimePoint;

    public async Task<IReadOnlyList<ScreenshotResult>> ExecuteAsync(
        VideoInfo video,
        CaptureOptions options,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var ext = _ffmpeg.GetOutputExtension(options.Format);
        var results = new List<ScreenshotResult>();
        var total = options.TimePoints.Count;

        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();

            var time = options.TimePoints[i];
            var fileName = $"tp_{TimeFormatter.ToFilenamePart(time)}{ext}";
            var outputPath = Path.Combine(outputDirectory, fileName);

            var cmd = new FFmpegCommandBuilder()
                .Input(video.FilePath)
                .SeekTo(time)
                .Frames(1)
                .Scale(options.ScaleWidth, options.ScaleHeight)
                .Format(options.Format, options.Quality)
                .Output(outputPath)
                .Build();

            await _ffmpeg.ExecuteAsync(cmd, ct: ct);

            if (File.Exists(outputPath))
            {
                var fi = new FileInfo(outputPath);
                results.Add(new ScreenshotResult
                {
                    ImagePath = outputPath,
                    Timestamp = time,
                    Width = 0,
                    Height = 0,
                    FileSizeBytes = fi.Length
                });
            }

            progress?.Report((double)(i + 1) / total);
        }

        return results;
    }
}
