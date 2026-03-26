namespace ShotCutter.Core.Models;

public sealed record ScreenshotTask
{
    public required VideoInfo Video { get; init; }
    public required CaptureOptions Options { get; init; }
    public required string OutputDirectory { get; init; }
}
