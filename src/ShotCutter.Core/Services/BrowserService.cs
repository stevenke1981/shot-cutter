using System.Diagnostics;

namespace ShotCutter.Core.Services;

public interface IBrowserService
{
    void OpenImage(string imagePath);
    void OpenHtmlGallery(IReadOnlyList<string> imagePaths, string galleryTitle = "ShotCutter Results");
}

public sealed class BrowserService : IBrowserService
{
    private readonly Func<string?> _browserPathFactory;

    public BrowserService(string? browserPath = null)
        : this(() => browserPath)
    {
    }

    public BrowserService(Func<string?> browserPathFactory)
    {
        _browserPathFactory = browserPathFactory;
    }

    public void OpenImage(string imagePath)
    {
        var uri = new Uri(imagePath).AbsoluteUri;
        LaunchUrl(uri);
    }

    public void OpenHtmlGallery(IReadOnlyList<string> imagePaths, string galleryTitle = "ShotCutter Results")
    {
        if (imagePaths.Count == 0) return;

        var tempDir = Path.Combine(Path.GetTempPath(), "ShotCutter");
        Directory.CreateDirectory(tempDir);
        var htmlPath = Path.Combine(tempDir, $"gallery_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var imageHtml = string.Join("\n", imagePaths.Select((p, i) =>
        {
            var uri = new Uri(p).AbsoluteUri;
            return $"""
                <div class="img-card">
                  <a href="{uri}" target="_blank">
                    <img src="{uri}" alt="Screenshot {i + 1}" loading="lazy" />
                  </a>
                  <p>{Path.GetFileName(p)}</p>
                </div>
            """;
        }));

        var html = $$"""
            <!DOCTYPE html>
            <html lang="zh-TW">
            <head>
              <meta charset="UTF-8"/>
              <title>{{galleryTitle}}</title>
              <style>
                body { font-family: 'Segoe UI', sans-serif; background: #1e1e2e; color: #cdd6f4; margin: 20px; }
                h1 { text-align: center; color: #89b4fa; }
                .gallery { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; padding: 16px; }
                .img-card { background: #313244; border-radius: 12px; overflow: hidden; transition: transform 0.2s; }
                .img-card:hover { transform: scale(1.02); }
                .img-card img { width: 100%; height: auto; display: block; }
                .img-card p { padding: 8px 12px; margin: 0; font-size: 13px; color: #a6adc8; word-break: break-all; }
                .count { text-align: center; color: #a6adc8; margin-bottom: 16px; }
              </style>
            </head>
            <body>
              <h1>{{galleryTitle}}</h1>
              <p class="count">共 {{imagePaths.Count}} 張截圖</p>
              <div class="gallery">
                {{imageHtml}}
              </div>
            </body>
            </html>
            """;

        File.WriteAllText(htmlPath, html);
        LaunchUrl(new Uri(htmlPath).AbsoluteUri);
    }

    private void LaunchUrl(string url)
    {
        var browserPath = _browserPathFactory();

        if (!string.IsNullOrEmpty(browserPath) && File.Exists(browserPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = browserPath,
                Arguments = $"\"{url}\"",
                UseShellExecute = false
            });
        }
        else
        {
            // Use system default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
