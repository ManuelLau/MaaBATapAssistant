using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;
using MaaBATapAssistant.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MaaBATapAssistant.Views;

public partial class TaskSettingsView
{
    private readonly SettingsViewModel _viewModel;

    public TaskSettingsView()
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
    }

    private void SettingsComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SettingsViewModel.UpdateConfigJsonFile();
    }

    private void ComboBoxPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 未展开时禁用鼠标滚动，以免误操作
        if (sender is ComboBox comboBox && !comboBox.IsDropDownOpen)
        {
            e.Handled = true;
        }
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
                Utility.CustomDebugWriteLine($"输入了错误的关卡 H{textBox.Text}");
                textBox.Clear();
            }
            ProgramDataModel.Instance.SettingsData.HardLevel = textBox.Text;
            SettingsViewModel.UpdateConfigJsonFile();
        }
    }
}
