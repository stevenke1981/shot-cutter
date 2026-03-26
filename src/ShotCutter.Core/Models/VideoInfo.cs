namespace ShotCutter.Core.Models;

public sealed record VideoInfo
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required TimeSpan Duration { get; init; }
    public required double FrameRate { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string CodecName { get; init; }
    public required long FileSizeBytes { get; init; }
    public IReadOnlyList<TimeSpan> KeyFrameTimestamps { get; init; } = [];
}
