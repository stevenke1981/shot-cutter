using ShotCutter.Core.Capture;
using ShotCutter.Core.Helpers;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;
using ShotCutter.SmartAnalysis.Models;

namespace ShotCutter.SmartAnalysis;

/// <summary>
/// Capture strategy that uses <see cref="ISceneAnalyzer"/> to detect scene boundaries,
/// then extracts a representative frame from each detected segment.
/// Registered as <see cref="CaptureMode.SmartScene"/>.
/// </summary>
public sealed class SmartCaptureStrategy : ICaptureStrategy
{
    private readonly IFFmpegService _ffmpeg;
    private readonly ISceneAnalyzer _analyzer;

    public SmartCaptureStrategy(IFFmpegService ffmpeg, ISceneAnalyzer analyzer)
    {
        _ffmpeg = ffmpeg;
        _analyzer = analyzer;
    }

    public CaptureMode Mode => CaptureMode.SmartScene;

    public async Task<IReadOnlyList<ScreenshotResult>> ExecuteAsync(
        VideoInfo video,
        CaptureOptions options,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        // Step 1 — analyse scene boundaries (runs ffmpeg internally, ~30% of the work)
        progress?.Report(0.05);

        var segments = await _analyzer.AnalyzeAsync(
            video.FilePath,
            options.SceneChangeThreshold,
            video.Duration,
            ct);

        progress?.Report(0.35);

        if (segments.Count == 0)
            return [];

        // Step 2 — capture one representative frame per segment
        var ext = _ffmpeg.GetOutputExtension(options.Format);
        var results = new List<ScreenshotResult>(segments.Count);
        var done = 0;

        foreach (var segment in segments)
        {
            ct.ThrowIfCancellationRequested();

            var ts = segment.RepresentativeTime;
            var fileName = $"scene_{ts.TotalSeconds:000000.000}{ext}".Replace('.', '_') + ext;
            // Sanitised filename: scene_000030_500.jpg
            fileName = $"scene_{(int)ts.TotalSeconds:D6}_{ts.Milliseconds:D3}{ext}";
            var outPath = Path.Combine(outputDirectory, fileName);

            var cmd = new FFmpegCommandBuilder()
                .SeekTo(ts)
                .Input(video.FilePath)
                .Frames(1)
                .Scale(options.ScaleWidth, options.ScaleHeight)
                .Format(options.Format, options.Quality)
                .Output(outPath)
                .Build();

            await _ffmpeg.ExecuteAsync(cmd, ct: ct);

            if (File.Exists(outPath))
            {
                var fileInfo = new FileInfo(outPath);
                results.Add(new ScreenshotResult
                {
                    ImagePath = outPath,
                    Timestamp = ts,
                    Width = 0,   // dimensions unknown without probing; acceptable for now
                    Height = 0,
                    FileSizeBytes = fileInfo.Length
                });
            }

            done++;
            progress?.Report(0.35 + 0.65 * done / segments.Count);
        }

        return results.AsReadOnly();
    }
}
