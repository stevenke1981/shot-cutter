using ShotCutter.Core.Models;

namespace ShotCutter.Core.Helpers;

public sealed class FFmpegCommandBuilder
{
    private string _inputPath = string.Empty;
    private string _outputPath = string.Empty;
    private string? _seekPosition;
    private string? _seekEof;
    private int? _frames;
    private readonly List<string> _videoFilters = [];
    private OutputFormat _format = OutputFormat.Jpeg;
    private int _quality = 85;
    private bool _overwrite = true;
    private bool _noAudio = true;

    public FFmpegCommandBuilder Input(string path)
    {
        _inputPath = path;
        return this;
    }

    public FFmpegCommandBuilder Output(string path)
    {
        _outputPath = path;
        return this;
    }

    public FFmpegCommandBuilder SeekTo(TimeSpan time)
    {
        _seekPosition = TimeFormatter.ToFFmpegTimestamp(time);
        return this;
    }

    public FFmpegCommandBuilder SeekFromEnd(double seconds)
    {
        _seekEof = $"-{seconds:F3}";
        return this;
    }

    public FFmpegCommandBuilder Frames(int count)
    {
        _frames = count;
        return this;
    }

    public FFmpegCommandBuilder AddVideoFilter(string filter)
    {
        _videoFilters.Add(filter);
        return this;
    }

    public FFmpegCommandBuilder Fps(double fps)
    {
        _videoFilters.Add($"fps={fps}");
        return this;
    }

    public FFmpegCommandBuilder SelectKeyFrames()
    {
        _videoFilters.Add("select='eq(pict_type\\,I)'");
        _videoFilters.Add("vsync=vfr");
        return this;
    }

    public FFmpegCommandBuilder SelectSceneChange(double threshold)
    {
        _videoFilters.Add($"select='gt(scene\\,{threshold:F2})'");
        _videoFilters.Add("vsync=vfr");
        _videoFilters.Add("showinfo");
        return this;
    }

    public FFmpegCommandBuilder Scale(int? width, int? height)
    {
        if (width.HasValue || height.HasValue)
        {
            var w = width?.ToString() ?? "-1";
            var h = height?.ToString() ?? "-1";
            _videoFilters.Add($"scale={w}:{h}");
        }
        return this;
    }

    public FFmpegCommandBuilder Format(OutputFormat format, int quality = 85)
    {
        _format = format;
        _quality = quality;
        return this;
    }

    public FFmpegCommandBuilder Overwrite(bool overwrite = true)
    {
        _overwrite = overwrite;
        return this;
    }

    public string Build()
    {
        if (string.IsNullOrEmpty(_inputPath))
            throw new InvalidOperationException("Input path is required.");
        if (string.IsNullOrEmpty(_outputPath))
            throw new InvalidOperationException("Output path is required.");

        var parts = new List<string>();

        if (_overwrite) parts.Add("-y");

        // Seek before input for fast seeking
        if (_seekPosition is not null)
        {
            parts.Add($"-ss {_seekPosition}");
        }

        if (_seekEof is not null)
        {
            parts.Add($"-sseof {_seekEof}");
        }

        parts.Add($"-i \"{_inputPath}\"");

        if (_noAudio) parts.Add("-an");

        if (_videoFilters.Count > 0)
        {
            parts.Add($"-vf \"{string.Join(",", _videoFilters)}\"");
        }

        // Format-specific quality
        switch (_format)
        {
            case OutputFormat.Jpeg:
                parts.Add($"-q:v {MapJpegQuality(_quality)}");
                break;
            case OutputFormat.Png:
                parts.Add("-compression_level 6");
                break;
            case OutputFormat.WebP:
                parts.Add($"-quality {_quality}");
                break;
        }

        if (_frames.HasValue)
        {
            parts.Add($"-frames:v {_frames.Value}");
        }

        parts.Add($"\"{_outputPath}\"");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Maps 0-100 quality to FFmpeg JPEG qscale (2=best, 31=worst).
    /// </summary>
    private static int MapJpegQuality(int quality)
    {
        var clamped = Math.Clamp(quality, 1, 100);
        return (int)Math.Round(2 + (100 - clamped) * 29.0 / 99.0);
    }
}
