using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace MaaBATapAssistant.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public List<string> ClientTypeSettingOptionsText { get; set; }
    public ObservableCollection<string> CafeInviteTimeSettingOptionsText { set; get; }
    public List<string> CafeInviteSortTypeSettingOptionsText { set; get; }
    public List<int> CafeInviteIndexSettingOptionsText { set; get; }
    public List<int> CafeApplyLayoutSettingOptionsText { set; get; }
    [ObservableProperty]
    public string cafe1ApplyLayoutAMText;
    [ObservableProperty]
    public string cafe1ApplyLayoutPMText;
    [ObservableProperty]
    public string cafe2ApplyLayoutAMText;
    [ObservableProperty]
    public string cafe2ApplyLayoutPMText;

    [ObservableProperty]
    public ProgramDataModel programData = ProgramDataModel.Instance;

    public SettingsViewModel()
    {
        // 初始化设置选项文本
        ClientTypeSettingOptionsText =
        [
            "官服",
            "B服",
            "国际服-繁体",
            "日服"
        ];
        CafeInviteTimeSettingOptionsText =
        [
            "4:00~15:59",
            "16:00~次日3:59",
            "立即，无视时间段",
            "不使用"
        ];
        CafeInviteSortTypeSettingOptionsText =
        [
            "好感等级由高到低",
            "好感等级由低到高",
            "精选(自上而下)"
        ];
        CafeInviteIndexSettingOptionsText = [1, 2, 3, 4, 5];
        CafeApplyLayoutSettingOptionsText = [1, 2, 3];
        Cafe1ApplyLayoutAMText = "4:00~15:59时间段1号咖啡厅应用家具预设序号";
        Cafe1ApplyLayoutPMText = "16:00~次日3:59时间段1号咖啡厅应用家具预设序号";
        Cafe2ApplyLayoutAMText = "4:00~15:59时间段2号咖啡厅应用家具预设序号";
        Cafe2ApplyLayoutPMText = "16:00~次日3:59时间段2号咖啡厅应用家具预设序号";

        //查找config.json,如果没有则使用默认的规则生成配置文件
        if (File.Exists(MyConstant.ConfigJsonFilePath))
        {
            LoadConfigJsonFile();
            UpdateConfigJsonFile();
        }
        else
        {
            Directory.CreateDirectory(MyConstant.ConfigJsonDirectory);
            File.Create(MyConstant.ConfigJsonFilePath).Close();
            UpdateConfigJsonFile();
        }
    }

    //从config.json文件中读取配置
    private static void LoadConfigJsonFile()
    {
        string settingsJson = File.ReadAllText(MyConstant.ConfigJsonFilePath);
        SettingsDataModel? settingsData = JsonConvert.DeserializeObject<SettingsDataModel>(settingsJson);
        if (settingsData == null)
        {
            throw new Exception("无法读取config.json");
        }
        ProgramDataModel.Instance.SettingsData = settingsData;
    }

    //更新配置文件，把当前的配置写入json
    public static void UpdateConfigJsonFile()
    {
        string formattedJson = JsonConvert.SerializeObject(ProgramDataModel.Instance.SettingsData, Formatting.Indented);
        File.WriteAllText(MyConstant.ConfigJsonFilePath, formattedJson);
    }

    // 更改客户端后刷新UI(时间段相关的文字)
    public void UpdateSettingUI()
    {
        int cafe1Index = ProgramDataModel.Instance.SettingsData.Cafe1InviteTimeSettingIndex;
        int cafe2Index = ProgramDataModel.Instance.SettingsData.Cafe2InviteTimeSettingIndex;
        switch ((EClientTypeSettingOptions)ProgramDataModel.Instance.SettingsData.ClientTypeSettingIndex)
        {
            default:
                CafeInviteTimeSettingOptionsText[0] = "4:00~15:59";
                CafeInviteTimeSettingOptionsText[1] = "16:00~次日3:59";
                Cafe1ApplyLayoutAMText = "4:00~15:59时间段1号咖啡厅应用家具预设序号";
                Cafe1ApplyLayoutPMText = "16:00~次日3:59时间段1号咖啡厅应用家具预设序号";
                Cafe2ApplyLayoutAMText = "4:00~15:59时间段2号咖啡厅应用家具预设序号";
                Cafe2ApplyLayoutPMText = "16:00~次日3:59时间段2号咖啡厅应用家具预设序号";
                break;
            case EClientTypeSettingOptions.Zh_TW:
            case EClientTypeSettingOptions.Jp:
                CafeInviteTimeSettingOptionsText[0] = "3:00~14:59";
                CafeInviteTimeSettingOptionsText[1] = "15:00~次日2:59";
                Cafe1ApplyLayoutAMText = "3:00~14:59时间段1号咖啡厅应用家具预设序号";
                Cafe1ApplyLayoutPMText = "15:00~次日2:59时间段1号咖啡厅应用家具预设序号";
                Cafe2ApplyLayoutAMText = "3:00~14:59时间段2号咖啡厅应用家具预设序号";
                Cafe2ApplyLayoutPMText = "15:00~次日2:59时间段2号咖啡厅应用家具预设序号";
                break;
        }
        ProgramDataModel.Instance.SettingsData.Cafe1InviteTimeSettingIndex = cafe1Index;
        ProgramDataModel.Instance.SettingsData.Cafe2InviteTimeSettingIndex = cafe2Index;
    }

    [RelayCommand]
    public static void OpenScreenshotFolder()
    {
        if (!Directory.Exists(MyConstant.ScreenshotImagePath))
        {
            Directory.CreateDirectory(MyConstant.ScreenshotImagePath);
        }
        Process.Start(new ProcessStartInfo("explorer.exe", MyConstant.ScreenshotImagePath)
        {
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public static void OpenUpdateWindow()
    {
        MainViewModel.Instance.OpenUpdateWindow();
    }

    [RelayCommand]
    public void SelectEmulatorPath()
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "Executable Files (*.exe)|*.exe",
            Title = "选择模拟器路径"
        };

        // 显示对话框并检查用户是否选择了文件
        if (openFileDialog.ShowDialog() == true)
        {
            ProgramData.SettingsData.EmulatorPath = openFileDialog.FileName;
            UpdateConfigJsonFile();
        }
    }

    [RelayCommand]
    public static void OpenAnnouncementWindow()
    {
        MainViewModel.Instance.OpenAnnouncementWindow();
    }
}
