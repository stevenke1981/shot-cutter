using ShotCutter.Core.Models;

namespace ShotCutter.Core.Tests;

public class BatchProgressTests
{
    private static BatchProgress Make(int completed, int total, double currentVideoProgress = 0.0,
        string currentVideoName = "test.mp4") =>
        new BatchProgress
        {
            CompletedVideos = completed,
            TotalVideos = total,
            CurrentVideoName = currentVideoName,
            CurrentVideoProgress = currentVideoProgress
        };

    [Fact]
    public void OverallProgress_WhenTotalIsZero_ReturnsZero()
    {
        var progress = Make(0, 0);
        Assert.Equal(0.0, progress.OverallProgress);
    }

    [Fact]
    public void OverallProgress_WhenNothingDone_ReturnsZero()
    {
        var progress = Make(0, 4, currentVideoProgress: 0.0);
        Assert.Equal(0.0, progress.OverallProgress);
    }

    [Fact]
    public void OverallProgress_WhenAllDone_ReturnsOne()
    {
        var progress = Make(4, 4, currentVideoProgress: 0.0);
        Assert.Equal(1.0, progress.OverallProgress);
    }

    [Fact]
    public void OverallProgress_WhenHalfDone_ReturnsHalf()
    {
        var progress = Make(2, 4, currentVideoProgress: 0.0);
        Assert.Equal(0.5, progress.OverallProgress);
    }

    [Fact]
    public void OverallProgress_IncludesCurrentVideoPartialProgress()
    {
        // 1 of 4 completed, current video is 50% done → (1 + 0.5) / 4 = 0.375
        var progress = Make(1, 4, currentVideoProgress: 0.5);
        Assert.Equal(0.375, progress.OverallProgress, precision: 10);
    }

    [Fact]
    public void OverallProgress_WhenOnlyCurrentVideoRunning_ReflectsItsProgress()
    {
        // 0 completed, current video is 75% done → 0.75 / 1 = 0.75
        var progress = Make(0, 1, currentVideoProgress: 0.75);
        Assert.Equal(0.75, progress.OverallProgress, precision: 10);
    }

    [Fact]
    public void OverallProgress_WhenAllCompletedAndCurrentAtOne_ExceedsOne()
    {
        // The engine increments completedCount before the final progress report,
        // so the last broadcast can briefly produce (total + 1.0) / total > 1.
        // UI bindings should clamp to 1.0 if needed.
        var progress = Make(4, 4, currentVideoProgress: 1.0);
        Assert.True(progress.OverallProgress > 1.0);
    }

    [Fact]
    public void BatchProgress_IsImmutableRecord()
    {
        var original = Make(1, 5, 0.5);
        var updated = original with { CompletedVideos = 2 };

        Assert.Equal(1, original.CompletedVideos);
        Assert.Equal(2, updated.CompletedVideos);
    }

    [Fact]
    public void TwoIdenticalBatchProgress_AreValueEqual()
    {
        var a = Make(2, 4, 0.5, "video.mp4");
        var b = Make(2, 4, 0.5, "video.mp4");

        Assert.Equal(a, b);
    }
}
