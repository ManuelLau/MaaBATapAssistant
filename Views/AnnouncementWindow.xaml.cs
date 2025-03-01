using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;
using MaaBATapAssistant.ViewModels;
using System.Windows;

namespace MaaBATapAssistant.Views;

public partial class AnnouncementWindow : Window
{
    public AnnouncementWindow()
    {
        InitializeComponent();
        DataContext = ProgramDataModel.Instance;

        MainTitle.Text = AnnouncementContent.MainTitle;
        Title0.Text = AnnouncementContent.Title0;
        Content0.Text = AnnouncementContent.Content0;
        Title1.Text = AnnouncementContent.Title1;
        Content1.Text = AnnouncementContent.Content1;
        //Title2.Text = AnnouncementContent.Title2;
        //Content2.Text = AnnouncementContent.Content2;
    }

    private void CheckBoxOnClick(object sender, RoutedEventArgs e)
    {
        SettingsViewModel.UpdateConfigJsonFile();
    }
}