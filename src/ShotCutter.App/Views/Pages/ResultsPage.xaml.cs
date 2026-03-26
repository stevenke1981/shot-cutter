using System.Windows.Controls;
using ShotCutter.App.ViewModels;

namespace ShotCutter.App.Views.Pages;

public partial class ResultsPage : Page
{
    public ResultsPage(ResultsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
