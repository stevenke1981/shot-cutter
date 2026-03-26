namespace ShotCutter.Core.Models;

public sealed record BatchProgress
{
    public required int CompletedVideos { get; init; }
    public required int TotalVideos { get; init; }
    public required string CurrentVideoName { get; init; }
    public required double CurrentVideoProgress { get; init; }

    public double OverallProgress => TotalVideos == 0
        ? 0
        : (CompletedVideos + CurrentVideoProgress) / TotalVideos;
}
