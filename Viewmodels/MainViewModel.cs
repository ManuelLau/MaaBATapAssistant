using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaBATapAssistant.Utils;
using MaaBATapAssistant.Models;
using MaaFramework.Binding;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using MaaBATapAssistant.Views;
using Newtonsoft.Json;
using System.IO;

namespace MaaBATapAssistant.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static MainViewModel? _instance;
    public static MainViewModel Instance
    {
        get => _instance ??= new();
    }
    [ObservableProperty]
    public ProgramDataModel programData = ProgramDataModel.Instance;
    [ObservableProperty]
    //等待执行的任务队列，该队列中的任务也可以选择是否显示在主界面任务列表内
    public ObservableCollection<TaskChainModel> waitingTaskList;
    [ObservableProperty]
    public ObservableCollection<MainViewLogItemModel> logDataList;
    [ObservableProperty]
    public bool isStoppingCurrentTask = false;
    private AnnouncementWindow? announcementWindow;

    [ObservableProperty]
    public string createTaskButtonText;
    [ObservableProperty]
    public string refreshTaskButtonText;

    public MainViewModel()
    {
        WaitingTaskList = [];
        LogDataList = [];
        CreateTaskButtonText = "生成任务";
        RefreshTaskButtonText = "刷新任务";
        if (!Models.ProgramDataModel.Instance.SettingsData.DoNotShowAnnouncementAgain)
            Application.Current.Dispatcher.BeginInvoke(new Action(OpenAnnouncementWindow), System.Windows.Threading.DispatcherPriority.Input);
    }

    [RelayCommand]
    public static void StartTaskButton()
    {
        TaskManager.Instance.Start();
    }

    [RelayCommand]
    public static void StopTaskButton()
    {
        TaskManager.Instance.Stop();
    }

    [RelayCommand]
    public static void CreateButton()
    {
        // 用户手动点击，先清空当前任务列表
        Instance.WaitingTaskList.Clear();
        DateTime nextCafeRefreshDateTime = TaskManager.Instance.CreateTask(DateTime.Now);
        TaskManager.Instance.CreateTask(nextCafeRefreshDateTime);
    }

    public void OpenAnnouncementWindow()
    {
        if (announcementWindow == null || !announcementWindow.IsVisible)
        {
            announcementWindow = new();
            announcementWindow.Closed += (s, args) => announcementWindow = null;
            announcementWindow.Left = Application.Current.MainWindow.Left + 50;
            announcementWindow.Top = Application.Current.MainWindow.Top + 30;
            announcementWindow.Show();
        }
        else
        {
            announcementWindow.Activate();
            announcementWindow.WindowState = WindowState.Normal;
        }
    }

    public void AppStart()
    {
        // 读取存储的任务列表
        if (File.Exists(MyConstant.CacheFilePath))
        {
            string json = File.ReadAllText(MyConstant.CacheFilePath);
            ObservableCollection<TaskChainModel>? deserializedCollection = JsonConvert.DeserializeObject<ObservableCollection<TaskChainModel>>(json);
            if (deserializedCollection != null)
                WaitingTaskList = deserializedCollection;
            foreach (var taskChainTtem in WaitingTaskList)
            {
                taskChainTtem.Status = ETaskChainStatus.Waiting;
            }
        }
        // 启动时自动检测更新
        if (ProgramData.SettingsData.IsAutoCheckAppUpdate)
        {
            Task.Run(async () =>
            {
                await Task.Delay(1000); // 延迟1秒
                await UpdateTool.CheckUpdate(false);
            });
        }
    }

    public void AppClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 存储当前任务列表
        string json = JsonConvert.SerializeObject(WaitingTaskList);
        File.WriteAllText(MyConstant.CacheFilePath, json);
        // 释放连接模拟器的资源
        TaskManager.Instance.MaaTaskerDispose();
    }

    [Obsolete]
    private async void AutoDetectDevice()
    {
        MaaToolkit _maaToolkit = new(true);//init: true
        var devices = await _maaToolkit.AdbDevice.FindAsync();
        if (devices.IsEmpty)
        {
            Utility.MyDebugWriteLine("找不到任何设备");
        }
        else
        {
            Utility.MyDebugWriteLine($"一共有{devices.MaaSizeCount}个Adb设备");
            foreach (var e in devices)
            {
                Utility.MyDebugWriteLine($"Name = {e.Name}\nAdbPath = {e.AdbPath}\nAdbSerial = {e.AdbSerial}\nConfig = {e.Config}");
            }
        }
    }
}