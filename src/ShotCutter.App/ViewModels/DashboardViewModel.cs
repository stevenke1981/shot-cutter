using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IFFprobeService _ffprobe;
    private readonly IScreenshotEngine _engine;
    private readonly ISettingsService _settings;
    private readonly IBrowserService _browser;

    private CancellationTokenSource? _cts;

    public DashboardViewModel(
        IFFprobeService ffprobe,
        IScreenshotEngine engine,
        ISettingsService settings,
        IBrowserService browser)
    {
        _ffprobe = ffprobe;
        _engine = engine;
        _settings = settings;
        _browser = browser;

        var appSettings = _settings.Load();
        SelectedMode = appSettings.DefaultMode;
        SelectedFormat = appSettings.DefaultFormat;
        Quality = appSettings.DefaultQuality;
    }

    // --- Video list ---
    public ObservableCollection<VideoInfo> Videos { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCaptureCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearVideosCommand))]
    private int _videoCount;

    // --- Capture options ---
    [ObservableProperty]
    private CaptureMode _selectedMode = CaptureMode.Interval;

    [ObservableProperty]
    private double _intervalSeconds = 1.0;

    [ObservableProperty]
    private string _timePointsText = "";

    [ObservableProperty]
    private double _sceneChangeThreshold = 0.3;

    [ObservableProperty]
    private bool _captureFirstFrame = true;

    [ObservableProperty]
    private bool _captureLastFrame = true;

    [ObservableProperty]
    private OutputFormat _selectedFormat = OutputFormat.Jpeg;

    [ObservableProperty]
    private int _quality = 85;

    [ObservableProperty]
    private string _outputDirectory = "";

    // --- Progress ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCaptureCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _statusText = "就緒";

    // --- Last results ---
    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<ScreenshotResult>>? _lastResults;

    // Commands
    [RelayCommand]
    private async Task AddVideosAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "選擇影片檔案",
            Multiselect = true,
            Filter = "影片檔案|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.ts;*.m4v|所有檔案|*.*"
        };

        var appSettings = _settings.Load();
        if (!string.IsNullOrEmpty(appSettings.LastInputDirectory)
            && Directory.Exists(appSettings.LastInputDirectory))
        {
            dialog.InitialDirectory = appSettings.LastInputDirectory;
        }

        if (dialog.ShowDialog() != true) return;

        foreach (var file in dialog.FileNames)
        {
            if (Videos.Any(v => v.FilePath == file)) continue;

            try
            {
                var info = await _ffprobe.GetVideoInfoAsync(file);
                Videos.Add(info);
            }
            catch (Exception ex)
            {
                StatusText = $"無法解析: {Path.GetFileName(file)} — {ex.Message}";
            }
        }

        VideoCount = Videos.Count;

        if (dialog.FileNames.Length > 0)
        {
            var dir = Path.GetDirectoryName(dialog.FileNames[0]);
            appSettings.LastInputDirectory = dir;
            _settings.Save(appSettings);
        }
    }

    [RelayCommand(CanExecute = nameof(HasVideos))]
    private void ClearVideos()
    {
        Videos.Clear();
        VideoCount = 0;
        StatusText = "已清空影片清單";
    }

    private bool HasVideos() => VideoCount > 0;

    [RelayCommand(CanExecute = nameof(CanStartCapture))]
    private async Task StartCaptureAsync()
    {
        if (IsRunning) return;

        var outputDir = GetOutputDirectory();
        if (string.IsNullOrEmpty(outputDir)) return;

        var options = BuildCaptureOptions();
        var tasks = Videos
            .Select(v => new ScreenshotTask
            {
                Video = v,
                Options = options,
                OutputDirectory = outputDir
            })
            .ToList();

        var appSettings = _settings.Load();

        _cts = new CancellationTokenSource();
        IsRunning = true;
        OverallProgress = 0;
        StatusText = "擷取中...";

        try
        {
            var progress = new Progress<BatchProgress>(p =>
            {
                OverallProgress = p.OverallProgress * 100;
                StatusText = $"[{p.CompletedVideos}/{p.TotalVideos}] {p.CurrentVideoName}";
            });

            LastResults = await _engine.ExecuteBatchAsync(
                tasks,
                appSettings.MaxParallelTasks,
                progress,
                _cts.Token);

            var totalScreenshots = LastResults.Sum(r => r.Count);
            StatusText = $"完成！共擷取 {totalScreenshots} 張截圖";
            OverallProgress = 100;

            if (appSettings.AutoOpenInBrowser && totalScreenshots > 0)
            {
                var allImages = LastResults.SelectMany(r => r.Select(s => s.ImagePath)).ToList();
                _browser.OpenHtmlGallery(allImages);
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "已取消擷取";
        }
        catch (Exception ex)
        {
            StatusText = $"錯誤: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private bool CanStartCapture() => VideoCount > 0 && !IsRunning;

    [RelayCommand]
    private void CancelCapture()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void BrowseOutputDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "選擇輸出資料夾"
        };

        if (!string.IsNullOrEmpty(OutputDirectory) && Directory.Exists(OutputDirectory))
        {
            dialog.InitialDirectory = OutputDirectory;
        }

        if (dialog.ShowDialog() == true)
        {
            OutputDirectory = dialog.FolderName;
        }
    }

    private string GetOutputDirectory()
    {
        if (!string.IsNullOrWhiteSpace(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
            return OutputDirectory;
        }

        // Default: same folder as the first video
        if (Videos.Count > 0)
        {
            var dir = Path.GetDirectoryName(Videos[0].FilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                var output = Path.Combine(dir, "ShotCutter_Output");
                Directory.CreateDirectory(output);
                return output;
            }
        }

        return "";
    }

    private CaptureOptions BuildCaptureOptions()
    {
        return new CaptureOptions
        {
            Mode = SelectedMode,
            IntervalSeconds = IntervalSeconds,
            TimePoints = ParseTimePoints(),
            SceneChangeThreshold = SceneChangeThreshold,
            CaptureFirstFrame = CaptureFirstFrame,
            CaptureLastFrame = CaptureLastFrame,
            Format = SelectedFormat,
            Quality = Quality,
        };
    }

    private IReadOnlyList<TimeSpan> ParseTimePoints()
    {
        if (string.IsNullOrWhiteSpace(TimePointsText)) return [];

        return TimePointsText
            .Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s =>
            {
                if (TimeSpan.TryParse(s, out var ts)) return ts;
                if (double.TryParse(s, out var sec)) return TimeSpan.FromSeconds(sec);
                return (TimeSpan?)null;
            })
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToList();
    }

    // Drag-drop support
    public void HandleFileDrop(string[] files)
    {
        _ = AddVideosFromPathsAsync(files);
    }

    private async Task AddVideosFromPathsAsync(string[] paths)
    {
        var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".ts", ".m4v"
        };

        foreach (var path in paths)
        {
            IEnumerable<string> files;
            if (Directory.Exists(path))
            {
                files = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => videoExtensions.Contains(Path.GetExtension(f)));
            }
            else if (File.Exists(path) && videoExtensions.Contains(Path.GetExtension(path)))
            {
                files = [path];
            }
            else
            {
                continue;
            }

            foreach (var file in files)
            {
                if (Videos.Any(v => v.FilePath == file)) continue;

                try
                {
                    var info = await _ffprobe.GetVideoInfoAsync(file);
                    Videos.Add(info);
                }
                catch (Exception ex)
                {
                    StatusText = $"無法解析: {Path.GetFileName(file)} — {ex.Message}";
                }
            }
        }

        VideoCount = Videos.Count;
    }

    public void RemoveVideo(VideoInfo video)
    {
        Videos.Remove(video);
        VideoCount = Videos.Count;
    }
}
