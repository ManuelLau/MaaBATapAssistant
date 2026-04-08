using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;
using MaaBATapAssistant.ViewModels;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MaaBATapAssistant.Views;

public partial class AppSettingsView
{
    private readonly SettingsViewModel _viewModel;

    public AppSettingsView()
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
    }

    private void DevicePathTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        ProgramDataModel.Instance.SettingsData.DevicePath = tb.Text;
        SettingsViewModel.UpdateConfigJsonFile();
    }

    private void AutoRunDeviceWaittingTimeSpanTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]$");
    }

    private void AutoRunDeviceWaittingTimeSpanTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        string text = tb.Text;
        // 等待时间最低10秒，最高600秒
        if (int.TryParse(text, out int value))
        {
            if (value > Constants.AutoRunDeviceWaittingMaxTimeSpan)
            {
                ProgramDataModel.Instance.SettingsData.AutoRunDeviceWaittingTimeSpan = Constants.AutoRunDeviceWaittingMaxTimeSpan;
                SettingsViewModel.UpdateConfigJsonFile();
            }
            else
            {
                ProgramDataModel.Instance.SettingsData.AutoRunDeviceWaittingTimeSpan = value;
                SettingsViewModel.UpdateConfigJsonFile();
            }
        }
    }

    private void AutoRunDeviceWaittingTimeSpanTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.Text = ProgramDataModel.Instance.SettingsData.AutoRunDeviceWaittingTimeSpan.ToString();
        if (int.TryParse(tb.Text, out int value))
        {
            if (value < Constants.AutoRunDeviceWaittingMinTimeSpan)
            {
                ProgramDataModel.Instance.SettingsData.AutoRunDeviceWaittingTimeSpan = Constants.AutoRunDeviceWaittingMinTimeSpan;
            }
        }
        else
        {
            // 如果输入的不是数字
            ProgramDataModel.Instance.SettingsData.AutoRunDeviceWaittingTimeSpan = Constants.AutoRunDeviceWaittingDefaultTimeSpan;
        }

        SettingsViewModel.UpdateConfigJsonFile();
    }

    private void BilibiliLinkMouseDown(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Constants.BilibiliLink,
            UseShellExecute = true
        });
    }

    private void BilibiliReadmeLinkMouseDown(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Constants.BilibiliReadmeLink,
            UseShellExecute = true
        });
    }
}
