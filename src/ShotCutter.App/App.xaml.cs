using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShotCutter.App.ViewModels;
using ShotCutter.App.Views.Pages;
using ShotCutter.Core.Capture;
using ShotCutter.Core.Services;
using ShotCutter.SmartAnalysis;

namespace ShotCutter.App;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Core services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IFFprobeService>(_ =>
                    new FFprobeService(ResolveToolPath("ffprobe")));
                services.AddSingleton<IFFmpegService>(_ =>
                    new FFmpegService(ResolveToolPath("ffmpeg")));
                services.AddSingleton<IBrowserService, BrowserService>();

                // Capture strategies
                services.AddSingleton<ICaptureStrategy>(sp =>
                    new IntervalCaptureStrategy(sp.GetRequiredService<IFFmpegService>()));
                services.AddSingleton<ICaptureStrategy>(sp =>
                    new TimePointCaptureStrategy(sp.GetRequiredService<IFFmpegService>()));
                services.AddSingleton<ICaptureStrategy>(sp =>
                    new KeyFrameCaptureStrategy(sp.GetRequiredService<IFFmpegService>()));
                services.AddSingleton<ICaptureStrategy>(sp =>
                    new SceneChangeCaptureStrategy(sp.GetRequiredService<IFFmpegService>()));
                services.AddSingleton<ICaptureStrategy>(sp =>
                    new FirstLastFrameStrategy(sp.GetRequiredService<IFFmpegService>()));

                // Smart analysis
                services.AddSingleton<ISceneAnalyzer>(sp =>
                    new HistogramSceneAnalyzer(sp.GetRequiredService<IFFmpegService>()));
                services.AddSingleton<ICaptureStrategy>(sp =>
                    new SmartCaptureStrategy(
                        sp.GetRequiredService<IFFmpegService>(),
                        sp.GetRequiredService<ISceneAnalyzer>()));

                services.AddSingleton<IScreenshotEngine, ScreenshotEngine>();

                // ViewModels
                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ResultsViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
                services.AddTransient<DashboardPage>();
                services.AddTransient<ResultsPage>();
                services.AddTransient<SettingsPage>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        MainWindow = mainWindow;

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Unexpected error:\n{e.Exception.Message}",
            "ShotCutter — Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static string ResolveToolPath(string toolName)
    {
        var exeName = toolName + ".exe";

        // 1. Bundled tools/ffmpeg/
        var bundled = Path.Combine(
            AppContext.BaseDirectory, "tools", "ffmpeg", exeName);
        if (File.Exists(bundled)) return bundled;

        // 2. Same directory as app
        var local = Path.Combine(AppContext.BaseDirectory, exeName);
        if (File.Exists(local)) return local;

        // 3. Fallback to PATH
        return exeName;
    }
}

