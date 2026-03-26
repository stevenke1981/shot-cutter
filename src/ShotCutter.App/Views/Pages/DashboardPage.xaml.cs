using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DashboardViewModel.SelectedVideo))
            {
                LoadPreview(_viewModel.SelectedVideo);
            }
        };
        LoadPreview(viewModel.SelectedVideo);
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

        LoadPreview(_viewModel.SelectedVideo);
    }

    private void VideoListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VideoListView.SelectedItem is VideoInfo video)
        {
            _viewModel.SelectedVideo = video;
        }

        LoadPreview(_viewModel.SelectedVideo);
    }

    private void PlayPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedVideo is null)
        {
            return;
        }

        EnsurePreviewSource(_viewModel.SelectedVideo);
        PreviewPlayer.Play();
    }

    private void PausePreview_Click(object sender, RoutedEventArgs e)
    {
        PreviewPlayer.Pause();
    }

    private void StopPreview_Click(object sender, RoutedEventArgs e)
    {
        PreviewPlayer.Stop();
    }

    private void PreviewPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _viewModel.StatusText = $"影片預覽失敗: {e.ErrorException.Message}";
    }

    private void LoadPreview(VideoInfo? video)
    {
        if (video is null)
        {
            PreviewPlayer.Stop();
            PreviewPlayer.Source = null;
            return;
        }

        EnsurePreviewSource(video);
        PreviewPlayer.Position = TimeSpan.Zero;
        PreviewPlayer.Pause();
    }

    private void EnsurePreviewSource(VideoInfo video)
    {
        var uri = new Uri(video.FilePath, UriKind.Absolute);
        if (PreviewPlayer.Source != uri)
        {
            PreviewPlayer.Source = uri;
        }
    }
}
