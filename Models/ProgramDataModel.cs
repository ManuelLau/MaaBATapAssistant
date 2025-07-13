using CommunityToolkit.Mvvm.ComponentModel;
using MaaBATapAssistant.Utils;
using System.ComponentModel;

namespace MaaBATapAssistant.Models;

#region 枚举声明
public enum EClientTypeSettingOptions
{
    [Description("官服")] Zh_CN = 0,
    [Description("B服")] Zh_CN_Bilibili,
    [Description("国际服-繁体中文")] Zh_TW,
    [Description("日服")] Jp,
    [Description("国际服-英文")] En,
};
public enum ECafeInviteTimeSettingOptions
{
    [Description("4:00~15:59")] AM = 0,
    [Description("16:00~次日3:59")] PM,
    [Description("立即，无视重置时间")] Immediately,
    [Description("不使用")] DoNotUse,
};
public enum ECafeInviteSortTypeSettingOptions
{
    [Description("好感由高到低")] BondLvFromHighToLow = 0,
    [Description("好感由低到高")] BondLvFromLowToHigh,
    [Description("精选排序")] Pinned,
};
public enum EDownloadSource
{
    Gitee,
    GitHub
}
#endregion

public partial class ProgramDataModel : ObservableObject
{
    private static readonly ProgramDataModel _instance = new();
    public static ProgramDataModel Instance
    {
        get => _instance ?? new();
    }

    /// <summary>挂机任务是否开始执行</summary>
    [ObservableProperty]
    public bool isAfkTaskRunning;
    /// <summary>当前任务是否正在执行</summary>
    [ObservableProperty]
    public bool isCurrentTaskExecuting;
    [ObservableProperty]
    public string updateInfo = string.Empty;
    [ObservableProperty]
    public EDownloadSource downloadSource;
    [ObservableProperty]
    public bool isCheckingNewVersion;
    [ObservableProperty]
    public bool hasNewVersion;
    [ObservableProperty]
    public bool isDownloadingFiles;
    [ObservableProperty]
    public bool isReadyForApplyUpdate;
    [ObservableProperty]
    public double downloadProgress;
    [ObservableProperty]
    public string downloadedSizeInfo = string.Empty;
    [ObservableProperty]
    public string resourcesVersion = "0.0.0.0";

    public SettingsDataModel SettingsData { get; set; }

    public ProgramDataModel()
    {
        IsAfkTaskRunning = false;
        IsCurrentTaskExecuting = false;
        DownloadSource = EDownloadSource.Gitee;
        IsCheckingNewVersion = false;
        HasNewVersion = false;
        IsDownloadingFiles = false;
        IsReadyForApplyUpdate = false;
        ResourcesVersion = ResourcesVersionInfo.GetResourcesVersion();

        SettingsData = new();
    }
}
