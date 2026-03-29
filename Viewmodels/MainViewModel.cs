using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaBATapAssistant.Utils;
using MaaBATapAssistant.Models;
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
    private UpdateWindow? updateWindow;

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
        if (!ProgramDataModel.Instance.SettingsData.DoNotShowAnnouncementAgain)
            Application.Current.Dispatcher.BeginInvoke(new Action(OpenAnnouncementWindow), System.Windows.Threading.DispatcherPriority.Input);
    }

    [RelayCommand]
    public static void StartTaskButton()
    {
        Utility.CustomDebugWriteLine("手动点击开始任务按钮");
        TaskManager.Instance.Start();
    }

    [RelayCommand]
    public static void StopTaskButton()
    {
        Utility.CustomDebugWriteLine("手动点击停止任务按钮");
        TaskManager.Instance.Stop(true);
    }

    [RelayCommand]
    public static void CreateButton()
    {
        Utility.CustomDebugWriteLine("手动点击生成/刷新任务按钮");
        // 用户手动点击，先清空当前任务列表
        Instance.WaitingTaskList.Clear();
        DateTime nextCafeRefreshDateTime = TaskManager.Instance.CreateTask(DateTime.Now);
        nextCafeRefreshDateTime = TaskManager.Instance.CreateTask(nextCafeRefreshDateTime);
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

    public void OpenUpdateWindow()
    {
        if (updateWindow == null || !updateWindow.IsVisible)
        {
            updateWindow = new();
            updateWindow.Closed += (s, args) => updateWindow = null;
            updateWindow.Left = Application.Current.MainWindow.Left + 190;
            updateWindow.Top = Application.Current.MainWindow.Top + 160;
            updateWindow.Show();
        }
        else
        {
            updateWindow.Activate();
            updateWindow.WindowState = WindowState.Normal;
        }
    }

    public void SetUpdateWindowTopmost(bool isTopmost)
    {
        if (updateWindow != null)
        {
            updateWindow.Topmost = isTopmost;
        }
    }

    public void AppStart()
    {
        // 读取存储的任务列表
        if (File.Exists(Constants.CacheFilePath))
        {
            string json = File.ReadAllText(Constants.CacheFilePath);
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
                UpdateTool.CheckNewVersion(true, out _, out _);
            });
        }
    }

    public void AppClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 存储当前任务列表
        string json = JsonConvert.SerializeObject(WaitingTaskList);
        File.WriteAllText(Constants.CacheFilePath, json);
        // 释放连接模拟器的资源
        TaskManager.Instance.DisposeMaaTasker();
        Utility.CustomDebugWriteLine("关闭程序");
    }
}