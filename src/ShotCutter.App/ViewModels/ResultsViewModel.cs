using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShotCutter.Core.Models;
using ShotCutter.Core.Services;

namespace ShotCutter.App.ViewModels;

public partial class ResultsViewModel : ObservableObject
{
    private readonly IBrowserService _browser;
    private readonly ISettingsService _settings;

    public ResultsViewModel(IBrowserService browser, ISettingsService settings)
    {
        _browser = browser;
        _settings = settings;
    }

    public ObservableCollection<ScreenshotResult> Screenshots { get; } = [];

    [ObservableProperty]
    private ScreenshotResult? _selectedScreenshot;

    [ObservableProperty]
    private string _statusText = "尚未擷取截圖";

    public void LoadResults(IReadOnlyList<IReadOnlyList<ScreenshotResult>> results)
    {
        Screenshots.Clear();

        foreach (var group in results)
        {
            foreach (var result in group)
            {
                Screenshots.Add(result);
            }
        }

        StatusText = $"共 {Screenshots.Count} 張截圖";
    }

    [RelayCommand]
    private void OpenInBrowser()
    {
        if (Screenshots.Count == 0) return;

        var images = Screenshots.Select(s => s.ImagePath).ToList();
        _browser.OpenHtmlGallery(images);
    }

    [RelayCommand]
    private void OpenSingleImage()
    {
        if (SelectedScreenshot is null) return;
        _browser.OpenImage(SelectedScreenshot.ImagePath);
    }
}
