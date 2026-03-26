using System.Globalization;
using System.Text.Json;
using ShotCutter.Core.Helpers;
using ShotCutter.Core.Models;

namespace ShotCutter.Core.Services;

public sealed class FFprobeService : IFFprobeService
{
    private readonly ProcessRunner _runner = new();
    private readonly Func<string> _ffprobePathFactory;

    public FFprobeService(string ffprobePath)
        : this(() => ffprobePath)
    {
    }

    public FFprobeService(Func<string> ffprobePathFactory)
    {
        _ffprobePathFactory = ffprobePathFactory;
    }

    public async Task<VideoInfo> GetVideoInfoAsync(string filePath, CancellationToken ct = default)
    {
        // Get basic video info as JSON
        var args = $"-v quiet -print_format json -show_format -show_streams -select_streams v:0 \"{filePath}\"";
        var result = await _runner.RunAsync(_ffprobePathFactory(), args, cancellationToken: ct);

        if (!result.Success)
            throw new InvalidOperationException($"FFprobe failed: {result.StandardError}");

        using var doc = JsonDocument.Parse(result.StandardOutput);
        var root = doc.RootElement;

        var stream = root.GetProperty("streams")[0];
        var format = root.GetProperty("format");

        var width = stream.GetProperty("width").GetInt32();
        var height = stream.GetProperty("height").GetInt32();
        var codecName = stream.GetProperty("codec_name").GetString() ?? "unknown";

        // Parse frame rate from "r_frame_rate" like "30000/1001"
        var frameRateStr = stream.GetProperty("r_frame_rate").GetString() ?? "30/1";
        var frameRate = ParseFraction(frameRateStr);

        // Duration from format
        var durationStr = format.GetProperty("duration").GetString() ?? "0";
        var duration = TimeSpan.FromSeconds(double.Parse(durationStr, CultureInfo.InvariantCulture));

        var fileSizeStr = format.GetProperty("size").GetString() ?? "0";
        var fileSize = long.Parse(fileSizeStr, CultureInfo.InvariantCulture);

        var fileName = Path.GetFileName(filePath);

        // Get keyframe timestamps
        var keyFrames = await GetKeyFrameTimestampsAsync(filePath, ct);

        return new VideoInfo
        {
            FilePath = filePath,
            FileName = fileName,
            Duration = duration,
            FrameRate = frameRate,
            Width = width,
            Height = height,
            CodecName = codecName,
            FileSizeBytes = fileSize,
            KeyFrameTimestamps = keyFrames
        };
    }

    private async Task<IReadOnlyList<TimeSpan>> GetKeyFrameTimestampsAsync(string filePath, CancellationToken ct)
    {
        var args = $"-v quiet -select_streams v:0 -show_entries packet=pts_time,flags -of csv=p=0 \"{filePath}\"";
        var result = await _runner.RunAsync(_ffprobePathFactory(), args, cancellationToken: ct);

        if (!result.Success)
            return [];

        var timestamps = new List<TimeSpan>();
        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split(',');
            if (parts.Length >= 2 && parts[1].Contains('K') &&
                double.TryParse(parts[0], CultureInfo.InvariantCulture, out var seconds))
            {
                timestamps.Add(TimeSpan.FromSeconds(seconds));
            }
        }

        return timestamps;
    }

    private static double ParseFraction(string fraction)
    {
        var parts = fraction.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], CultureInfo.InvariantCulture, out var num) &&
            double.TryParse(parts[1], CultureInfo.InvariantCulture, out var den) &&
            den > 0)
        {
            return num / den;
        }

        return double.TryParse(fraction, CultureInfo.InvariantCulture, out var val) ? val : 30.0;
    }
}
