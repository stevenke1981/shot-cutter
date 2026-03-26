using ShotCutter.Core.Models;

namespace ShotCutter.Core.Capture;

public interface ICaptureStrategy
{
    CaptureMode Mode { get; }

    Task<IReadOnlyList<ScreenshotResult>> ExecuteAsync(
        VideoInfo video,
        CaptureOptions options,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
