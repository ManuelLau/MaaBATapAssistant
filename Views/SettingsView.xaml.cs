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

    private void AvoidingStartTimeTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        System.Windows.Controls.TextBox? textBox = sender as System.Windows.Controls.TextBox;
        string? text = textBox?.Text;
        bool isSuccess = TimeOnly.TryParse(text, out TimeOnly parsedTime);
        if (isSuccess)
        {
            ProgramDataModel.Instance.SettingsData.AvoidingStartTime = parsedTime;
        }
        SettingsViewModel.UpdateConfigJsonFile();
    }
    private void AvoidingEndTimeTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        System.Windows.Controls.TextBox? textBox = sender as System.Windows.Controls.TextBox;
        string? text = textBox?.Text;
        bool isSuccess = TimeOnly.TryParse(text, out TimeOnly parsedTime);
        if (isSuccess)
        {
            ProgramDataModel.Instance.SettingsData.AvoidingEndTime = parsedTime;
        }
        SettingsViewModel.UpdateConfigJsonFile();
    }

    private void HardLevelTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox)
        {
            var regex = new Regex(@"^(99|[1-9][0-9]?)-[1-3]$");
            if (!regex.IsMatch(textBox.Text))
            {
                HandyControl.Controls.MessageBox.Error("请输入正确的困难关卡");
                Utility.MyDebugWriteLine($"输入了错误的关卡 H{textBox.Text}");
                textBox.Clear();
            }
            ProgramDataModel.Instance.SettingsData.HardLevel = textBox.Text;
            SettingsViewModel.UpdateConfigJsonFile();
        }
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
            UseShellExecute = true
        });
    }

    private void BilibiliReadmeLinkMouseDown(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = MyConstant.BilibiliReadmeLink,
            UseShellExecute = true
        });
    }
}
