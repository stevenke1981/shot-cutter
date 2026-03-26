using ShotCutter.Core.Helpers;
using ShotCutter.Core.Models;

namespace ShotCutter.Core.Services;

public interface IFFmpegService
{
    Task<ProcessResult> ExecuteAsync(
        string arguments,
        Action<string>? onStdErr = null,
        CancellationToken ct = default);

    string GetOutputExtension(OutputFormat format);
}

public sealed class FFmpegService : IFFmpegService
{
    private readonly ProcessRunner _runner = new();
    private readonly string _ffmpegPath;

    public FFmpegService(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    public async Task<ProcessResult> ExecuteAsync(
        string arguments,
        Action<string>? onStdErr = null,
        CancellationToken ct = default)
    {
        return await _runner.RunAsync(_ffmpegPath, arguments, onStdErr, ct);
    }

    public string GetOutputExtension(OutputFormat format) => format switch
    {
        OutputFormat.Jpeg => ".jpg",
        OutputFormat.Png => ".png",
        OutputFormat.WebP => ".webp",
        _ => ".jpg"
    };
}
