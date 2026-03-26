using Moq;
using ShotCutter.Core.Capture;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.Core.Tests;

public class ScreenshotEngineTests
{
    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────
    private static VideoInfo MakeVideo(string name = "test.mp4") => new VideoInfo
    {
        FilePath = $@"C:\Videos\{name}",
        FileName = name,
        Duration = TimeSpan.FromMinutes(5),
        FrameRate = 30,
        Width = 1920,
        Height = 1080,
        CodecName = "h264",
        FileSizeBytes = 1024 * 1024 * 100
    };

    private static ScreenshotResult MakeResult(string path = @"C:\out\img.jpg") => new ScreenshotResult
    {
        ImagePath = path,
        Timestamp = TimeSpan.FromSeconds(1),
        Width = 1920,
        Height = 1080,
        FileSizeBytes = 1024 * 50
    };

    private static Mock<ICaptureStrategy> MakeStrategyMock(
        CaptureMode mode,
        IReadOnlyList<ScreenshotResult>? results = null)
    {
        var mock = new Mock<ICaptureStrategy>();
        mock.Setup(s => s.Mode).Returns(mode);
        mock.Setup(s => s.ExecuteAsync(
                It.IsAny<VideoInfo>(),
                It.IsAny<CaptureOptions>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<double>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(results ?? new[] { MakeResult() });
        return mock;
    }

    // ──────────────────────────────────────────────
    // Tests
    // ──────────────────────────────────────────────
    [Fact]
    public async Task ExecuteBatchAsync_DispatchesCorrectStrategy_ForEachTask()
    {
        var intervalMock = MakeStrategyMock(CaptureMode.Interval);
        var keyFrameMock = MakeStrategyMock(CaptureMode.KeyFrame);

        var engine = new ScreenshotEngine(new ICaptureStrategy[]
        {
            intervalMock.Object,
            keyFrameMock.Object
        });

        var tasks = new[]
        {
            new ScreenshotTask
            {
                Video = MakeVideo("video1.mp4"),
                Options = new CaptureOptions { Mode = CaptureMode.Interval },
                OutputDirectory = @"C:\Output"
            },
            new ScreenshotTask
            {
                Video = MakeVideo("video2.mp4"),
                Options = new CaptureOptions { Mode = CaptureMode.KeyFrame },
                OutputDirectory = @"C:\Output"
            }
        };

        await engine.ExecuteBatchAsync(tasks, maxParallel: 1);

        intervalMock.Verify(s => s.ExecuteAsync(
            It.Is<VideoInfo>(v => v.FileName == "video1.mp4"),
            It.IsAny<CaptureOptions>(),
            It.IsAny<string>(),
            It.IsAny<IProgress<double>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        keyFrameMock.Verify(s => s.ExecuteAsync(
            It.Is<VideoInfo>(v => v.FileName == "video2.mp4"),
            It.IsAny<CaptureOptions>(),
            It.IsAny<string>(),
            It.IsAny<IProgress<double>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ReturnsResultsFromEachStrategy()
    {
        var result1 = MakeResult(@"C:\out\img1.jpg");
        var result2 = MakeResult(@"C:\out\img2.jpg");

        var strategy = MakeStrategyMock(CaptureMode.Interval, new[] { result1, result2 });
        var engine = new ScreenshotEngine(new[] { strategy.Object });

        var tasks = new[]
        {
            new ScreenshotTask
            {
                Video = MakeVideo(),
                Options = new CaptureOptions { Mode = CaptureMode.Interval },
                OutputDirectory = @"C:\Output"
            }
        };

        var allResults = await engine.ExecuteBatchAsync(tasks, maxParallel: 1);

        Assert.Single(allResults);
        Assert.Equal(2, allResults[0].Count);
        Assert.Equal(@"C:\out\img1.jpg", allResults[0][0].ImagePath);
        Assert.Equal(@"C:\out\img2.jpg", allResults[0][1].ImagePath);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WhenModeNotRegistered_ThrowsNotSupportedException()
    {
        var strategy = MakeStrategyMock(CaptureMode.Interval);
        var engine = new ScreenshotEngine(new[] { strategy.Object });

        var tasks = new[]
        {
            new ScreenshotTask
            {
                Video = MakeVideo(),
                Options = new CaptureOptions { Mode = CaptureMode.KeyFrame }, // not registered
                OutputDirectory = @"C:\Output"
            }
        };

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            engine.ExecuteBatchAsync(tasks, maxParallel: 1));
    }

    [Fact]
    public async Task ExecuteBatchAsync_EmptyTaskList_ReturnsEmptyList()
    {
        var strategy = MakeStrategyMock(CaptureMode.Interval);
        var engine = new ScreenshotEngine(new[] { strategy.Object });

        var allResults = await engine.ExecuteBatchAsync([], maxParallel: 1);

        Assert.Empty(allResults);
    }

    [Fact]
    public async Task ExecuteBatchAsync_CancellationToken_CancelsExecution()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // pre-cancelled

        var strategy = MakeStrategyMock(CaptureMode.Interval);
        var engine = new ScreenshotEngine(new[] { strategy.Object });

        var tasks = new[]
        {
            new ScreenshotTask
            {
                Video = MakeVideo(),
                Options = new CaptureOptions { Mode = CaptureMode.Interval },
                OutputDirectory = @"C:\Output"
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            engine.ExecuteBatchAsync(tasks, maxParallel: 1, ct: cts.Token));
    }

    [Fact]
    public async Task ExecuteBatchAsync_ReportsProgressThroughCompletion()
    {
        var strategy = MakeStrategyMock(CaptureMode.Interval);
        var engine = new ScreenshotEngine(new[] { strategy.Object });
        var reports = new List<BatchProgress>();

        var tasks = new[]
        {
            new ScreenshotTask
            {
                Video = MakeVideo(),
                Options = new CaptureOptions { Mode = CaptureMode.Interval },
                OutputDirectory = @"C:\Output"
            }
        };

        var progress = new Progress<BatchProgress>(p => reports.Add(p));

        await engine.ExecuteBatchAsync(tasks, maxParallel: 1, progress: progress);

        // Allow async progress callbacks to flush
        await Task.Delay(50);

        // At least one final completion report should have TotalVideos=1
        Assert.NotEmpty(reports);
        Assert.All(reports, r => Assert.Equal(1, r.TotalVideos));
    }

    [Fact]
    public async Task ExecuteBatchAsync_MultipleTasks_OutputDirectoryContainsVideoName()
    {
        string? capturedOutputDir = null;

        var strategyMock = new Mock<ICaptureStrategy>();
        strategyMock.Setup(s => s.Mode).Returns(CaptureMode.Interval);
        strategyMock
            .Setup(s => s.ExecuteAsync(
                It.IsAny<VideoInfo>(),
                It.IsAny<CaptureOptions>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<double>>(),
                It.IsAny<CancellationToken>()))
            .Callback<VideoInfo, CaptureOptions, string, IProgress<double>?, CancellationToken>(
                (_, _, dir, _, _) => capturedOutputDir = dir)
            .ReturnsAsync(Array.Empty<ScreenshotResult>());

        var engine = new ScreenshotEngine(new[] { strategyMock.Object });

        var tasks = new[]
        {
            new ScreenshotTask
            {
                Video = MakeVideo("my_video.mp4"),
                Options = new CaptureOptions { Mode = CaptureMode.Interval },
                OutputDirectory = @"C:\Output"
            }
        };

        await engine.ExecuteBatchAsync(tasks, maxParallel: 1);

        Assert.NotNull(capturedOutputDir);
        Assert.Contains("my_video", capturedOutputDir);
    }
}
