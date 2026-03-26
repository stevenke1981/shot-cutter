using ShotCutter.Core.Capture;
using ShotCutter.Core.Models;

namespace ShotCutter.Core.Services;

public interface IScreenshotEngine
{
    Task<IReadOnlyList<IReadOnlyList<ScreenshotResult>>> ExecuteBatchAsync(
        IReadOnlyList<ScreenshotTask> tasks,
        int maxParallel,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default);
}

public sealed class ScreenshotEngine : IScreenshotEngine
{
    private readonly IReadOnlyDictionary<CaptureMode, ICaptureStrategy> _strategies;

    public ScreenshotEngine(IEnumerable<ICaptureStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Mode);
    }

    public async Task<IReadOnlyList<IReadOnlyList<ScreenshotResult>>> ExecuteBatchAsync(
        IReadOnlyList<ScreenshotTask> tasks,
        int maxParallel,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(Math.Max(1, maxParallel));
        var allResults = new IReadOnlyList<ScreenshotResult>[tasks.Count];
        var completedCount = 0;

        var taskList = tasks.Select((task, index) => Task.Run(async () =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!_strategies.TryGetValue(task.Options.Mode, out var strategy))
                    throw new NotSupportedException($"Capture mode '{task.Options.Mode}' is not supported.");

                var videoProgress = new Progress<double>(p =>
                {
                    progress?.Report(new BatchProgress
                    {
                        CompletedVideos = completedCount,
                        TotalVideos = tasks.Count,
                        CurrentVideoName = task.Video.FileName,
                        CurrentVideoProgress = p
                    });
                });

                var videoOutputDir = Path.Combine(
                    task.OutputDirectory,
                    SanitizeFileName(Path.GetFileNameWithoutExtension(task.Video.FileName)));

                var results = await strategy.ExecuteAsync(
                    task.Video, task.Options, videoOutputDir, videoProgress, ct);

                allResults[index] = results;
                Interlocked.Increment(ref completedCount);

                progress?.Report(new BatchProgress
                {
                    CompletedVideos = completedCount,
                    TotalVideos = tasks.Count,
                    CurrentVideoName = task.Video.FileName,
                    CurrentVideoProgress = 1.0
                });
            }
            finally
            {
                semaphore.Release();
            }
        }, ct)).ToArray();

        await Task.WhenAll(taskList);

        return allResults.Select(r => r ?? (IReadOnlyList<ScreenshotResult>)[]).ToList();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
