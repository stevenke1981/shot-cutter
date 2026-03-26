using CommunityToolkit.Mvvm.ComponentModel;

namespace ShotCutter.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "ShotCutter";
}
