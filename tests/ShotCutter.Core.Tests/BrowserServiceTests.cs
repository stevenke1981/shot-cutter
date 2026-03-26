using ShotCutter.Core.Services;

namespace ShotCutter.Core.Tests;

/// <summary>
/// Tests for BrowserService that do NOT actually open a browser.
/// We verify file creation and HTML content; LaunchUrl is exercised only indirectly.
/// </summary>
public class BrowserServiceTests : IDisposable
{
    // Collect any HTML gallery files created so we can clean up afterwards.
    private readonly List<string> _createdFiles = [];
    private readonly string _galleryDir = Path.Combine(Path.GetTempPath(), "ShotCutter");

    public void Dispose()
    {
        foreach (var f in _createdFiles)
        {
            try { File.Delete(f); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Snapshot the gallery directory before and after to discover newly created files.
    /// </summary>
    private IReadOnlyList<string> GetNewGalleryFiles(Action action)
    {
        Directory.CreateDirectory(_galleryDir);
        var before = Directory.GetFiles(_galleryDir, "gallery_*.html").ToHashSet();

        action();

        var after = Directory.GetFiles(_galleryDir, "gallery_*.html");
        var newFiles = after.Where(f => !before.Contains(f)).ToList();
        _createdFiles.AddRange(newFiles);
        return newFiles;
    }

    [Fact]
    public void OpenHtmlGallery_EmptyList_DoesNotCreateFile()
    {
        // Use a non-existent browser path so LaunchUrl exits immediately
        var service = new BrowserService(browserPath: "nonexistent_browser.exe");

        var newFiles = GetNewGalleryFiles(() =>
            service.OpenHtmlGallery([]));

        Assert.Empty(newFiles);
    }

    [Fact]
    public void OpenHtmlGallery_SingleImage_CreatesHtmlFile()
    {
        var service = new BrowserService(browserPath: "nonexistent_browser.exe");
        var imagePaths = new[] { @"C:\fake\screenshot_001.jpg" };

        var newFiles = GetNewGalleryFiles(() =>
            service.OpenHtmlGallery(imagePaths, "Test Gallery"));

        Assert.Single(newFiles);
    }

    [Fact]
    public void OpenHtmlGallery_CreatedHtml_ContainsTitleAndImages()
    {
        var service = new BrowserService(browserPath: "nonexistent_browser.exe");
        var imagePaths = new[]
        {
            @"C:\fake\img001.jpg",
            @"C:\fake\img002.jpg",
            @"C:\fake\img003.jpg"
        };

        var newFiles = GetNewGalleryFiles(() =>
            service.OpenHtmlGallery(imagePaths, "My Test Gallery"));

        Assert.Single(newFiles);
        var html = File.ReadAllText(newFiles[0]);

        Assert.Contains("My Test Gallery", html);
        Assert.Contains("img001.jpg", html);
        Assert.Contains("img002.jpg", html);
        Assert.Contains("img003.jpg", html);
        Assert.Contains("共 3 張截圖", html);
    }

    [Fact]
    public void OpenHtmlGallery_CreatedHtml_IsValidHtmlDocument()
    {
        var service = new BrowserService(browserPath: "nonexistent_browser.exe");
        var imagePaths = new[] { @"C:\fake\shot.png" };

        var newFiles = GetNewGalleryFiles(() =>
            service.OpenHtmlGallery(imagePaths));

        var html = File.ReadAllText(newFiles[0]);
        Assert.StartsWith("<!DOCTYPE html>", html.TrimStart());
        Assert.Contains("</html>", html);
        Assert.Contains("<meta charset=\"UTF-8\"", html);
    }

    [Fact]
    public void OpenHtmlGallery_CreatedFilename_ContainsDatePattern()
    {
        var service = new BrowserService(browserPath: "nonexistent_browser.exe");
        var before = DateTime.Now;

        var newFiles = GetNewGalleryFiles(() =>
            service.OpenHtmlGallery(new[] { @"C:\fake\x.jpg" }));

        var fileName = Path.GetFileName(newFiles[0]);
        // gallery_yyyyMMdd_HHmmss.html
        Assert.Matches(@"gallery_\d{8}_\d{6}\.html", fileName);
    }
}
