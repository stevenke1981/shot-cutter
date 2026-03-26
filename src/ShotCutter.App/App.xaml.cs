using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShotCutter.App.Services;
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
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IFFprobeService>(sp =>
                    new FFprobeService(() =>
                    {
                        var settings = sp.GetRequiredService<ISettingsService>().Load();
                        return ToolPathResolver.ResolveFfprobePath(settings.FFprobePath);
                    }));
                services.AddSingleton<IFFmpegService>(sp =>
                    new FFmpegService(() =>
                    {
                        var settings = sp.GetRequiredService<ISettingsService>().Load();
                        return ToolPathResolver.ResolveFfmpegPath(settings.FFmpegPath);
                    }));
                services.AddSingleton<IBrowserService>(sp =>
                    new BrowserService(() =>
                    {
                        var settings = sp.GetRequiredService<ISettingsService>().Load();
                        return ToolPathResolver.ResolveBrowserPath(settings.BrowserPath);
                    }));
                services.AddSingleton<ICaptureResultsStore, CaptureResultsStore>();

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
}

