using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;
using Newtonsoft.Json;
using System.IO;

namespace MaaBATapAssistant.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public List<string>? ClientTypeSettingOptionsText { get; set; }
    public List<string>? CafeInviteTimeSettingOptionsText { set; get; }
    public List<string>? CafeInviteSortTypeSettingOptionsText { set; get; }
    public List<int>? CafeInviteIndexSettingOptionsText { set; get; }
    public List<int>? CafeApplyLayoutSettingOptionsText { set; get; }
    [ObservableProperty]
    public ProgramDataModel programData = ProgramDataModel.Instance;
    [ObservableProperty]
    public string projectUrl = MyConstant.ProjectUrl;

    public SettingsViewModel()
    {
        InitSettingsOptionsText();

        //查找config.json,如果没有则使用默认的规则生成配置文件
        if (File.Exists(MyConstant.ConfigJsonFilePath))
        {
            LoadConfigJsonFile();
        }
        else
        {
            Directory.CreateDirectory(MyConstant.ConfigJsonDirectory);
            File.Create(MyConstant.ConfigJsonFilePath).Close();
            UpdateConfigJsonFile();
        }
    }

    //设置里下拉选项显示的文本
    private void InitSettingsOptionsText()
    {
        ClientTypeSettingOptionsText =
        [
            "官服",
            "B服",
            "国际服-繁体",
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

    [RelayCommand]
    public static async Task CheckUpdate()
    {
        await UpdateTool.CheckUpdate(true);
    }

    [RelayCommand]
    public static void OpenAnnouncementWindow()
    {
        MainViewModel.Instance.OpenAnnouncementWindow();
    }
}
