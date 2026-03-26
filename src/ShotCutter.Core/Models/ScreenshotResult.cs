namespace ShotCutter.Core.Models;

public sealed record ScreenshotResult
{
    public required string ImagePath { get; init; }
    public required TimeSpan Timestamp { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long FileSizeBytes { get; init; }
}
