using CommunityToolkit.Mvvm.ComponentModel;
using MaaBATapAssistant.Utils;

namespace MaaBATapAssistant.Models;

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
