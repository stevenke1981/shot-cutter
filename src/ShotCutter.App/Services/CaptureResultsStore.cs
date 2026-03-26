using ShotCutter.Core.Models;

namespace ShotCutter.App.Services;

public interface ICaptureResultsStore
{
    IReadOnlyList<IReadOnlyList<ScreenshotResult>> CurrentResults { get; }
    void SetResults(IReadOnlyList<IReadOnlyList<ScreenshotResult>> results);
}

public sealed class CaptureResultsStore : ICaptureResultsStore
{
    private IReadOnlyList<IReadOnlyList<ScreenshotResult>> _currentResults = [];

    public IReadOnlyList<IReadOnlyList<ScreenshotResult>> CurrentResults => _currentResults;

    public void SetResults(IReadOnlyList<IReadOnlyList<ScreenshotResult>> results)
    {
        _currentResults = results;
    }
}
