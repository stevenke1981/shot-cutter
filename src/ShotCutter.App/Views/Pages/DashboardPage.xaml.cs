using System.Windows;
using System.Windows.Controls;
using ShotCutter.App.ViewModels;
using ShotCutter.Core.Models;

namespace ShotCutter.App.Views.Pages;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            _viewModel.HandleFileDrop(files);
        }
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = VideoListView.SelectedItems.Cast<VideoInfo>().ToList();
        foreach (var video in selected)
        {
            _viewModel.RemoveVideo(video);
        }
    }
}
