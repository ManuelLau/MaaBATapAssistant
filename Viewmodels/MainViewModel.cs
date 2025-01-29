using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaBATapAssistant.Utils;
using MaaBATapAssistant.Models;
using MaaFramework.Binding;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using MaaBATapAssistant.Views;

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
    public ObservableCollection<string> logDataList;
    [ObservableProperty]
    public bool isStoppingCurrentTask = false;
    private AnnouncementWindow? announcementWindow;

    public MainViewModel()
    {
        waitingTaskList = [];
        LogDataList = [];
        if (!Models.ProgramDataModel.Instance.SettingsData.DoNotShowAnnouncementAgain)
            Application.Current.Dispatcher.BeginInvoke(new Action(OpenAnnouncementWindow), System.Windows.Threading.DispatcherPriority.Input);
    }

    /// <summary>打印Log到程序界面上</summary>
    public void PrintLog(string content)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogDataList.Add(DateTime.Now.ToString("MM/dd HH:mm:ss") + "   " + content);
        });
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
        TaskManager.Instance.CreateTask();
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

    public void DeleteTaskChain(TaskChainModel item)
    {
        WaitingTaskList.Remove(item);
        TaskManager.Instance.RemoveFromCurrentTaskQueue(item);
    }

    [Obsolete]
    private async void AutoDetectDevice()
    {
        MaaToolkit _maaToolkit = new(true);//init: true
        var devices = await _maaToolkit.AdbDevice.FindAsync();
        if (devices.IsEmpty is true)
        {
            MessageBox.Show("is empty");
        }
        else
        {
            MessageBox.Show($"一共有{devices.MaaSizeCount}个Adb设备");
            foreach (var e in devices)
            {
                MessageBox.Show($"{e.Name}\n{e.AdbPath}\n{e.AdbSerial}\n{e.Config}");
            }
        }
    }
}