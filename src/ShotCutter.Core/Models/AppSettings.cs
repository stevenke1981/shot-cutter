using System.Text.Json.Serialization;

namespace ShotCutter.Core.Models;

public sealed class AppSettings
{
    public string? LastInputDirectory { get; set; }
    public string? LastOutputDirectory { get; set; }
    public string? FFmpegPath { get; set; }
    public string? FFprobePath { get; set; }
    public CaptureMode DefaultMode { get; set; } = CaptureMode.Interval;
    public OutputFormat DefaultFormat { get; set; } = OutputFormat.Jpeg;
    public int DefaultQuality { get; set; } = 85;
    public int MaxParallelTasks { get; set; } = 2;
    public bool AutoOpenInBrowser { get; set; }
    public string? BrowserPath { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BrowserSendMode BrowserSendMode { get; set; } = BrowserSendMode.HtmlGallery;
}

public enum BrowserSendMode
{
    SingleImage,
    HtmlGallery
}
