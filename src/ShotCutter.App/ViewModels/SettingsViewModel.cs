using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private AppSettings _appSettings;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _appSettings = _settingsService.Load();

        FFmpegPath = _appSettings.FFmpegPath ?? "";
        FFprobePath = _appSettings.FFprobePath ?? "";
        MaxParallelTasks = _appSettings.MaxParallelTasks;
        AutoOpenInBrowser = _appSettings.AutoOpenInBrowser;
        BrowserPath = _appSettings.BrowserPath ?? "";
        BrowserSendMode = _appSettings.BrowserSendMode;
        DefaultMode = _appSettings.DefaultMode;
        DefaultFormat = _appSettings.DefaultFormat;
        DefaultQuality = _appSettings.DefaultQuality;
    }

    [ObservableProperty] private string _fFmpegPath;
    [ObservableProperty] private string _fFprobePath;
    [ObservableProperty] private int _maxParallelTasks;
    [ObservableProperty] private bool _autoOpenInBrowser;
    [ObservableProperty] private string _browserPath;
    [ObservableProperty] private BrowserSendMode _browserSendMode;
    [ObservableProperty] private CaptureMode _defaultMode;
    [ObservableProperty] private OutputFormat _defaultFormat;
    [ObservableProperty] private int _defaultQuality;

    [ObservableProperty]
    private string _statusText = "";

    [RelayCommand]
    private void BrowseFFmpeg()
    {
        var path = BrowseExecutable("選擇 ffmpeg.exe", "ffmpeg.exe");
        if (path is not null) FFmpegPath = path;
    }

    [RelayCommand]
    private void BrowseFFprobe()
    {
        var path = BrowseExecutable("選擇 ffprobe.exe", "ffprobe.exe");
        if (path is not null) FFprobePath = path;
    }

    [RelayCommand]
    private void BrowseBrowser()
    {
        var path = BrowseExecutable("選擇瀏覽器", "*.exe");
        if (path is not null) BrowserPath = path;
    }

    [RelayCommand]
    private void Save()
    {
        _appSettings.FFmpegPath = string.IsNullOrWhiteSpace(FFmpegPath) ? null : FFmpegPath;
        _appSettings.FFprobePath = string.IsNullOrWhiteSpace(FFprobePath) ? null : FFprobePath;
        _appSettings.MaxParallelTasks = Math.Clamp(MaxParallelTasks, 1, 8);
        _appSettings.AutoOpenInBrowser = AutoOpenInBrowser;
        _appSettings.BrowserPath = string.IsNullOrWhiteSpace(BrowserPath) ? null : BrowserPath;
        _appSettings.BrowserSendMode = BrowserSendMode;
        _appSettings.DefaultMode = DefaultMode;
        _appSettings.DefaultFormat = DefaultFormat;
        _appSettings.DefaultQuality = Math.Clamp(DefaultQuality, 1, 100);

        _settingsService.Save(_appSettings);
        StatusText = "設定已儲存";
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        _appSettings = new AppSettings();
        _settingsService.Save(_appSettings);

        FFmpegPath = "";
        FFprobePath = "";
        MaxParallelTasks = 2;
        AutoOpenInBrowser = false;
        BrowserPath = "";
        BrowserSendMode = BrowserSendMode.HtmlGallery;
        DefaultMode = CaptureMode.Interval;
        DefaultFormat = OutputFormat.Jpeg;
        DefaultQuality = 85;

        StatusText = "已恢復預設值";
    }

    private static string? BrowseExecutable(string title, string fileName)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = $"執行檔|{fileName}|所有檔案|*.*"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
