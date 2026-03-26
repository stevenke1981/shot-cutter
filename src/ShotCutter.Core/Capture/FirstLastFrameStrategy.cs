using ShotCutter.Core.Helpers;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.Core.Capture;

/// <summary>
/// Captures the first frame and/or last frame of the video.
/// </summary>
public sealed class FirstLastFrameStrategy : ICaptureStrategy
{
    private readonly IFFmpegService _ffmpeg;

    public FirstLastFrameStrategy(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public CaptureMode Mode => CaptureMode.FirstLastFrame;

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
        var steps = (options.CaptureFirstFrame ? 1 : 0) + (options.CaptureLastFrame ? 1 : 0);
        var done = 0;

        // First frame
        if (options.CaptureFirstFrame)
        {
            ct.ThrowIfCancellationRequested();
            var outputPath = Path.Combine(outputDirectory, $"first_frame{ext}");

            var cmd = new FFmpegCommandBuilder()
                .Input(video.FilePath)
                .SeekTo(TimeSpan.Zero)
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
                    Timestamp = TimeSpan.Zero,
                    Width = 0,
                    Height = 0,
                    FileSizeBytes = fi.Length
                });
            }

            done++;
            progress?.Report(steps > 0 ? (double)done / steps : 1.0);
        }

        // Last frame
        if (options.CaptureLastFrame)
        {
            ct.ThrowIfCancellationRequested();
            var outputPath = Path.Combine(outputDirectory, $"last_frame{ext}");

            var cmd = new FFmpegCommandBuilder()
                .Input(video.FilePath)
                .SeekFromEnd(0.5) // seek to 0.5s before end to ensure we get last frame
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
                    Timestamp = video.Duration,
                    Width = 0,
                    Height = 0,
                    FileSizeBytes = fi.Length
                });
            }

            done++;
            progress?.Report(steps > 0 ? (double)done / steps : 1.0);
        }

        return results;
    }
}
