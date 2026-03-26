using ShotCutter.Core.Models;

namespace ShotCutter.Core.Tests;

public class CaptureOptionsTests
{
    [Fact]
    public void DefaultCaptureOptions_HaveExpectedValues()
    {
        var options = new CaptureOptions();

        Assert.Equal(CaptureMode.Interval, options.Mode);
        Assert.Equal(1.0, options.IntervalSeconds);
        Assert.Equal(0.3, options.SceneChangeThreshold);
        Assert.True(options.CaptureFirstFrame);
        Assert.True(options.CaptureLastFrame);
        Assert.Equal(OutputFormat.Jpeg, options.Format);
        Assert.Equal(85, options.Quality);
        Assert.Empty(options.TimePoints);
        Assert.Null(options.ScaleWidth);
        Assert.Null(options.ScaleHeight);
    }

    [Fact]
    public void CaptureOptions_CanBeCreatedWithRecordSyntax()
    {
        var options = new CaptureOptions
        {
            Mode = CaptureMode.SceneChange,
            SceneChangeThreshold = 0.5,
            Format = OutputFormat.Png,
            Quality = 95
        };

        Assert.Equal(CaptureMode.SceneChange, options.Mode);
        Assert.Equal(0.5, options.SceneChangeThreshold);
        Assert.Equal(OutputFormat.Png, options.Format);
        Assert.Equal(95, options.Quality);
    }

    [Fact]
    public void CaptureOptions_WithTimePoints_StoredCorrectly()
    {
        var timePoints = new List<TimeSpan>
        {
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(30)),
            TimeSpan.FromHours(1)
        };

        var options = new CaptureOptions
        {
            Mode = CaptureMode.TimePoint,
            TimePoints = timePoints.AsReadOnly()
        };

        Assert.Equal(3, options.TimePoints.Count);
        Assert.Equal(TimeSpan.FromSeconds(10), options.TimePoints[0]);
        Assert.Equal(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(30)), options.TimePoints[1]);
        Assert.Equal(TimeSpan.FromHours(1), options.TimePoints[2]);
    }

    [Theory]
    [InlineData(CaptureMode.Interval)]
    [InlineData(CaptureMode.TimePoint)]
    [InlineData(CaptureMode.KeyFrame)]
    [InlineData(CaptureMode.SceneChange)]
    [InlineData(CaptureMode.FirstLastFrame)]
    [InlineData(CaptureMode.SmartScene)]
    public void AllCaptureModes_AreDefinedInEnum(CaptureMode mode)
    {
        Assert.True(Enum.IsDefined(typeof(CaptureMode), mode));
    }

    [Theory]
    [InlineData(OutputFormat.Jpeg)]
    [InlineData(OutputFormat.Png)]
    [InlineData(OutputFormat.WebP)]
    public void AllOutputFormats_AreDefinedInEnum(OutputFormat format)
    {
        Assert.True(Enum.IsDefined(typeof(OutputFormat), format));
    }
}
