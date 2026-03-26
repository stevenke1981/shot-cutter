using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.Core.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempPath;

    public SettingsServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"ShotCutterTests_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath))
            File.Delete(_tempPath);
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        var service = new SettingsService(_tempPath);

        var settings = service.Load();

        Assert.NotNull(settings);
        Assert.Equal(CaptureMode.Interval, settings.DefaultMode);
        Assert.Equal(OutputFormat.Jpeg, settings.DefaultFormat);
        Assert.Equal(85, settings.DefaultQuality);
        Assert.Equal(2, settings.MaxParallelTasks);
        Assert.False(settings.AutoOpenInBrowser);
        Assert.Equal(BrowserSendMode.HtmlGallery, settings.BrowserSendMode);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsAllSettings()
    {
        var service = new SettingsService(_tempPath);
        var original = new AppSettings
        {
            LastInputDirectory = @"C:\Videos",
            LastOutputDirectory = @"C:\Output",
            FFmpegPath = @"C:\ffmpeg\ffmpeg.exe",
            FFprobePath = @"C:\ffmpeg\ffprobe.exe",
            DefaultMode = CaptureMode.SceneChange,
            DefaultFormat = OutputFormat.Png,
            DefaultQuality = 95,
            MaxParallelTasks = 4,
            AutoOpenInBrowser = true,
            BrowserSendMode = BrowserSendMode.SingleImage
        };

        service.Save(original);
        var loaded = service.Load();

        Assert.Equal(original.LastInputDirectory, loaded.LastInputDirectory);
        Assert.Equal(original.LastOutputDirectory, loaded.LastOutputDirectory);
        Assert.Equal(original.FFmpegPath, loaded.FFmpegPath);
        Assert.Equal(original.FFprobePath, loaded.FFprobePath);
        Assert.Equal(original.DefaultMode, loaded.DefaultMode);
        Assert.Equal(original.DefaultFormat, loaded.DefaultFormat);
        Assert.Equal(original.DefaultQuality, loaded.DefaultQuality);
        Assert.Equal(original.MaxParallelTasks, loaded.MaxParallelTasks);
        Assert.True(loaded.AutoOpenInBrowser);
        Assert.Equal(BrowserSendMode.SingleImage, loaded.BrowserSendMode);
    }

    [Fact]
    public void Save_WritesJsonFile()
    {
        var service = new SettingsService(_tempPath);

        service.Save(new AppSettings { DefaultQuality = 75 });

        Assert.True(File.Exists(_tempPath));
        var json = File.ReadAllText(_tempPath);
        Assert.Contains("defaultQuality", json);
        Assert.Contains("75", json);
    }

    [Fact]
    public void Load_WhenFileIsCorrupted_ReturnsDefaultSettings()
    {
        File.WriteAllText(_tempPath, "{ invalid json {{{{");
        var service = new SettingsService(_tempPath);

        var settings = service.Load();

        Assert.NotNull(settings);
        Assert.Equal(85, settings.DefaultQuality);
    }

    [Fact]
    public void Load_WhenFileIsEmpty_ReturnsDefaultSettings()
    {
        File.WriteAllText(_tempPath, string.Empty);
        var service = new SettingsService(_tempPath);

        var settings = service.Load();

        Assert.NotNull(settings);
    }

    [Fact]
    public void Save_ThenLoad_CaptureModeEnum_RoundTrips()
    {
        var service = new SettingsService(_tempPath);

        foreach (var mode in Enum.GetValues<CaptureMode>())
        {
            service.Save(new AppSettings { DefaultMode = mode });
            var loaded = service.Load();
            Assert.Equal(mode, loaded.DefaultMode);
        }
    }
}
