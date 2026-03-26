using System.Windows.Controls;
using ShotCutter.App.ViewModels;

namespace ShotCutter.App.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
