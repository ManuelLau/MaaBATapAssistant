using MaaBATapAssistant.Models;
using MaaBATapAssistant.ViewModels;

namespace MaaBATapAssistant.Views;

public partial class UpdateWindow
{
    private readonly UpdateViewModel _viewModel;

    public UpdateWindow()
    {
        InitializeComponent();
        _viewModel = new UpdateViewModel();
        DataContext = _viewModel;

        if (ProgramDataModel.Instance.DownloadSource == EDownloadSource.Gitee)
        {
            ApiOption0.IsChecked = true;
            ApiOption1.IsChecked = false;
        }
        else
        {
            ApiOption0.IsChecked = false;
            ApiOption1.IsChecked = true;
        }
    }

    private void ApiButtonChecked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (ApiOption0.IsChecked == true)
        {
            ProgramDataModel.Instance.DownloadSource = EDownloadSource.Gitee;
        }
        else if (ApiOption1.IsChecked == true)
        {
            ProgramDataModel.Instance.DownloadSource = EDownloadSource.GitHub;
        }
    }
}
