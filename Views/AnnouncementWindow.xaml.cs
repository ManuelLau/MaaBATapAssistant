using MaaBATapAssistant.ViewModels;
using System.Windows;

namespace MaaBATapAssistant.Views;

public partial class AnnouncementWindow : Window
{
    public AnnouncementWindow()
    {
        InitializeComponent();
        DataContext = AnnouncementViewModel.Instance;
    }

    private void CheckBoxOnClick(object sender, RoutedEventArgs e)
    {
        SettingsViewModel.UpdateConfigJsonFile();
    }
}