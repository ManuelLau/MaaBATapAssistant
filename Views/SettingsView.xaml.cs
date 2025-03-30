using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;
using MaaBATapAssistant.ViewModels;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

    private void EmulatorPathTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        ProgramDataModel.Instance.SettingsData.EmulatorPath = tb.Text;
        SettingsViewModel.UpdateConfigJsonFile();
    }

    private void AutoRunEmulatorWaittingTimeSpanTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]$");
    }

    private void AutoRunEmulatorWaittingTimeSpanTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        string text = tb.Text;
        // 等待时间最低10秒，最高600秒
        if (int.TryParse(text, out int value))
        {
            if (value > MyConstant.AutoRunEmulatorWaittingMaxTimeSpan)
            {
                ProgramDataModel.Instance.SettingsData.AutoRunEmulatorWaittingTimeSpan = MyConstant.AutoRunEmulatorWaittingMaxTimeSpan;
                SettingsViewModel.UpdateConfigJsonFile();
            }
            else
            {
                ProgramDataModel.Instance.SettingsData.AutoRunEmulatorWaittingTimeSpan = value;
                SettingsViewModel.UpdateConfigJsonFile();
            }
        }
    }

    private void AutoRunEmulatorWaittingTimeSpanTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.Text = ProgramDataModel.Instance.SettingsData.AutoRunEmulatorWaittingTimeSpan.ToString();
        if (int.TryParse(tb.Text, out int value))
        {
            if (value < MyConstant.AutoRunEmulatorWaittingMinTimeSpan)
            {
                ProgramDataModel.Instance.SettingsData.AutoRunEmulatorWaittingTimeSpan = MyConstant.AutoRunEmulatorWaittingMinTimeSpan;
            }
        }
        else
        {
            // 如果输入的不是数字
            ProgramDataModel.Instance.SettingsData.AutoRunEmulatorWaittingTimeSpan = MyConstant.AutoRunEmulatorWaittingDefaultTimeSpan;
        }

        SettingsViewModel.UpdateConfigJsonFile();
    }

    private void BilibiliLinkMouseDown(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = MyConstant.BilibiliLink,
            UseShellExecute = true // 使用默认浏览器打开
        });
    }
}
