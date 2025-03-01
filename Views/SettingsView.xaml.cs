using MaaBATapAssistant.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MaaBATapAssistant.Views;

public partial class SettingsView
{
    private readonly SettingsViewModel _viewModel;

    public SettingsView()
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
    }

    private void SettingsComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SettingsViewModel.UpdateConfigJsonFile();
    }

    private void SettingsClientTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SettingsViewModel.UpdateConfigJsonFile();
        _viewModel.UpdateSettingUI();
    }

    private void SettingsCheckBoxClick(object sender, RoutedEventArgs e)
    {
        SettingsViewModel.UpdateConfigJsonFile();
    }
}
