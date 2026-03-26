using ShotCutter.App.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ShotCutter.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
    {
        DataContext = viewModel;
        InitializeComponent();

        RootNavigation.SetServiceProvider(serviceProvider);
    }
}