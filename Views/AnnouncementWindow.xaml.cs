using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;
using MaaBATapAssistant.ViewModels;
using System.Diagnostics;
using System.Windows;

namespace MaaBATapAssistant.Views;

public partial class AnnouncementWindow : Window
{
    public AnnouncementWindow()
    {
        InitializeComponent();
        DataContext = ProgramDataModel.Instance;

        MainTitle.Text = AnnouncementContent.MainTitle;
        LatestUpdateTitle.Text = AnnouncementContent.LatestUpdateTitle;
        LatestUpdateContent.Text = AnnouncementContent.LatestUpdateContent;
        NotesTitle.Text = AnnouncementContent.NotesTitle;
        NotesContent.Text = AnnouncementContent.NotesContent;
        UpdateHistory.Text = AnnouncementContent.UpdateHistory;
        UpdateHistoryTitle0.Text = AnnouncementContent.UpdateHistoryTitle0;
        UpdateHistoryContent0.Text = AnnouncementContent.UpdateHistoryContent0;
    }

    private void CheckBoxOnClick(object sender, RoutedEventArgs e)
    {
        SettingsViewModel.UpdateConfigJsonFile();
    }

    private void BilibiliReadmeLinkMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = MyConstant.BilibiliReadmeLink,
            UseShellExecute = true
        });
    }
}