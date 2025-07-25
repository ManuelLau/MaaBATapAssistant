﻿using MaaBATapAssistant.ViewModels;
using MaaBATapAssistant.Models;
using MaaFramework.Binding;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading;
using static MaaBATapAssistant.Utils.CustomTask;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MaaBATapAssistant.Utils;

public class TaskManager
{
    /// <summary>当前任务链是否需要打印任务结束的Log，包括成功、失败、无效</summary>
    public bool CurrentTaskChainPrintFinishedLog;
    private static TaskManager? _instance;
    public static TaskManager Instance
    {
        get => _instance ??= new();
    }
    private readonly MaaToolkit _maaToolkit;
    private MaaTasker? _maaTasker;
    //用于界面显示的任务列表，包含执行中的、待机的任务
    private readonly ObservableCollection<TaskChainModel> _waitingTaskChainList = MainViewModel.Instance.WaitingTaskList;
    //要马上执行的任务队列，执行前会进行模拟器连接、启动游戏的流程，执行后进行返回主界面
    private readonly List<TaskChainModel> _currentTaskChainList = [];
    private readonly SettingsDataModel _settingsData = ProgramDataModel.Instance.SettingsData;
    private CancellationTokenSource _cancellationTokenSource;

    public TaskManager()
    {
        _maaToolkit = new MaaToolkit(true);
        _cancellationTokenSource = new();
    }

    public async void Start()
    {
        if (_waitingTaskChainList.Count <= 0)
        {
            Utility.PrintError("暂无任务，请先生成任务");
            return;
        }
        // 如果下一个时间段没有除了重启之外的任务，则提醒用户手动刷新
        DateTime nextCafeRefreshDateTime = GetNextCafeRefreshDateTime(DateTime.Now);
        DateTime previousCafeRefreshDateTime = nextCafeRefreshDateTime.AddHours(-12);
        DateTime nextNextCafeRefreshDateTime = nextCafeRefreshDateTime.AddHours(12);
        bool thisPeriodHasTask = false;
        bool nextPeriodHasTask = false;
        for (int i = 0; i < _waitingTaskChainList.Count; i++)
        {
            if (_waitingTaskChainList[i].TaskQueue.Peek().Type != ETaskType.RestartGame)
            {
                if (_waitingTaskChainList[i].ExecuteDateTime >= previousCafeRefreshDateTime
                                && _waitingTaskChainList[i].ExecuteDateTime < nextCafeRefreshDateTime)
                {
                    thisPeriodHasTask = true;
                }
                if (_waitingTaskChainList[i].ExecuteDateTime >= nextCafeRefreshDateTime
                                && _waitingTaskChainList[i].ExecuteDateTime < nextNextCafeRefreshDateTime)
                {
                    nextPeriodHasTask = true;
                }
            }
        }
        if (!thisPeriodHasTask)
        {
            Utility.PrintError("注意：当前时间段没有任务");
        }
        if (!nextPeriodHasTask)
        {
            Utility.PrintError("注意：下一个时间段没有任务");
        }

        ProgramDataModel.Instance.IsAfkTaskRunning = true;
        _cancellationTokenSource = new();
        await Task.Run(async () =>
        {
            // 如果填写了模拟器路径，则开始任务时候不检测模拟器是否连接
            if (string.IsNullOrEmpty(_settingsData.EmulatorPath))
            {
                if (!await AutoConnect(_cancellationTokenSource.Token))
                {
                    Stop(false);
                    return;
                }
            }
            PreventSleepTool.PreventSleep();
            await StartAfkTask(_cancellationTokenSource.Token);
        });
    }

    /// <summary>
    /// 点击停止任务按钮，可能任务在执行中
    /// </summary>
    /// <param name="isOutOfAfkTask">是否正在执行当前任务</param>
    public async void Stop(bool isExecutingCurrentTask)
    {
        if (isExecutingCurrentTask)
        {
            Utility.PrintLog("正在停止任务...");
        }
        MainViewModel.Instance.IsStoppingCurrentTask = true;
        foreach (var taskChainTtem in _waitingTaskChainList)
        {
            taskChainTtem.Status = ETaskChainStatus.Waiting;
        }
        PreventSleepTool.RestoreSleep();
        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (Exception e)
        {
            Utility.MyDebugWriteLine("Stop()-_cancellationTokenSource.Cancel() failed!");
            Utility.MyDebugWriteLine($"Error: {e.Message}");
        }
        await Task.Run(MaaTaskerDispose);
        if (!isExecutingCurrentTask)
        {
            StopCurrentTask();
        }
    }

    //生成从给定时间到下一次咖啡厅刷新时间前的任务
    public DateTime CreateTask(DateTime taskDateTime)
    {
        // 处理规避时间段。如果end time小于start time则需要额外生成一个end time
        DateTime AvoidingStartDateTime = new(taskDateTime.Year, taskDateTime.Month, taskDateTime.Day,
                _settingsData.AvoidingStartTime.Hour, _settingsData.AvoidingStartTime.Minute, _settingsData.AvoidingStartTime.Second);
        DateTime AvoidingEndDateTime = new(taskDateTime.Year, taskDateTime.Month, taskDateTime.Day,
                _settingsData.AvoidingEndTime.Hour, _settingsData.AvoidingEndTime.Minute, _settingsData.AvoidingEndTime.Second);
        DateTime AvoidingEndDateTimeNextDay = DateTime.MinValue;
        if (_settingsData.AvoidingStartTime > _settingsData.AvoidingEndTime)
        {
            AvoidingEndDateTimeNextDay = AvoidingEndDateTime.AddDays(1);
        }

        DateTime nextServerRefreshDateTime = GetNextServerRefreshDateTime(taskDateTime); //04:00 or 03:00
        DateTime nextCafeRefreshDateTime = GetNextCafeRefreshDateTime(taskDateTime);
        bool hasCreatedCafe1InviteTask = false;
        bool hasCreatedCafe2InviteTask = false;
        bool hasCreatedCafe1AMApplyLayoutTask = false;
        bool hasCreatedCafe1PMApplyLayoutTask = false;
        bool hasCreatedCafe2AMApplyLayoutTask = false;
        bool hasCreatedCafe2PMApplyLayoutTask = false;
        bool hasCreatedSweepHardLevelTask = false;
        //循环生成第一轮任务(直到下一次咖啡厅刷新时间)
        do
        {
            if (_settingsData.IsCreateTaskAvoidSpecifiedTime)
            {
                if (_settingsData.AvoidingStartTime > _settingsData.AvoidingEndTime)
                {
                    if (taskDateTime < AvoidingEndDateTime)
                    {
                        taskDateTime = AvoidingEndDateTime;
                    }
                    else if (taskDateTime >= AvoidingStartDateTime && taskDateTime < AvoidingEndDateTimeNextDay)
                    {
                        taskDateTime = AvoidingEndDateTimeNextDay;
                    }
                }
                else
                {
                    if (taskDateTime >= AvoidingStartDateTime && taskDateTime < AvoidingEndDateTime)
                    {
                        taskDateTime = AvoidingEndDateTime;
                    }
                }
                if (taskDateTime >= nextCafeRefreshDateTime)
                {
                    break;
                }
            }
            CreateTaskFragment(taskDateTime, ref hasCreatedCafe1InviteTask, ref hasCreatedCafe2InviteTask, ref hasCreatedCafe1AMApplyLayoutTask,
            ref hasCreatedCafe1PMApplyLayoutTask, ref hasCreatedCafe2AMApplyLayoutTask, ref hasCreatedCafe2PMApplyLayoutTask, ref hasCreatedSweepHardLevelTask);
            taskDateTime = taskDateTime.AddHours(3);
        } while (taskDateTime < nextCafeRefreshDateTime);
        taskDateTime = nextCafeRefreshDateTime;
        //如果时间到达服务器刷新时间，就添加一个重启任务
        if (taskDateTime == nextServerRefreshDateTime)
        {
            Queue<TaskModel> taskQueue = new();
            taskQueue.Enqueue(new("重启游戏", "RestartGame", string.Empty, ETaskType.RestartGame));
            AddToWaitingTaskList(new("重启游戏", taskDateTime, ETaskChainType.System, true, true, "日期已变更，重启游戏中...", taskQueue));
        }
        return taskDateTime;
        //对任务按时间排序?
    }

    //自动连接模拟器和Maa资源，默认选择搜索到的第一个模拟器
    private async Task<bool> AutoConnect(CancellationToken token)
    {
        var devices = _maaToolkit.AdbDevice.Find();
        if (devices.IsEmpty)
        {
            devices = await AutoRunEmulator(token);
        }
        if (devices == null || token.IsCancellationRequested)
        {
            Utility.MyDebugWriteLine($"自动打开模拟器失败-AutoRunEmulator()-token.IsCancellationRequested:{token.IsCancellationRequested}");
            return false;
        }

        if (!LoadMaaSource(out MaaResource? maaResource) || maaResource == null)
        {
            return false;
        }

        Utility.PrintLog("正在连接模拟器...");
        try
        {
            _maaTasker = new()
            {
                //默认使用第0个模拟器，后续再改To-do
                Controller = devices[0].ToAdbController(),
                Resource = maaResource,
                DisposeOptions = DisposeOptions.All,
            };
            // 注册自定义识别、动作
            _maaTasker.Resource.Register(new RelationshipRankUpScreenshot());
            _maaTasker.Resource.Register(new InviteUnavailableSkipTask());
            _maaTasker.Resource.Register(new InviteScreenshot());
            _maaTasker.Resource.Register(new InviteCancelNotify());
            _maaTasker.Resource.Register(new DuplicatedLoginStopTask());
            _maaTasker.Resource.Register(new MaintenanceStopTask());
            _maaTasker.Resource.Register(new ClientUpdateStopTask());
            _maaTasker.Resource.Register(new IPBlockedStopTask());
            _maaTasker.Resource.Register(new PrintSweepError());
            _maaTasker.Resource.Register(new PrintFindeLevelError());
            _maaTasker.Resource.Register(new SweepDropScreenshot());
        }
        catch (Exception e)
        {
            Utility.PrintLog("模拟器初始化失败，可能是ADB连接出现问题，请尝试重启模拟器、ADB或者重启系统");
            Utility.MyDebugWriteLine($"Error: {e.Message}");
            return false;
        }
        if (_maaTasker == null || !_maaTasker.IsInitialized)
        {
            Utility.PrintLog("模拟器初始化失败，可能是ADB连接出现问题，请尝试重启模拟器、ADB或者重启系统");
            return false;
        }
        Utility.PrintLog("成功连接至模拟器" + devices[0].Name);
        return true;
    }

    private async Task<MaaFramework.Binding.Buffers.IMaaListBuffer<AdbDeviceInfo>?> AutoRunEmulator(CancellationToken token)
    {
        if (string.IsNullOrEmpty(_settingsData.EmulatorPath))
        {
            Utility.PrintError("未设置模拟器路径，请先设置模拟器路径或手动打开模拟器");
        }
        else if (!System.IO.Path.Exists(_settingsData.EmulatorPath))
        {
            Utility.PrintError("自动连接模拟器失败，请检查路径是否正确");
            Utility.MyDebugWriteLine(_settingsData.EmulatorPath);
        }
        else
        {
            try
            {
                Utility.MyDebugWriteLine("找不到模拟器，将自动打开模拟器：" + _settingsData.EmulatorPath);
                Utility.PrintLog("正在打开模拟器...");
                Process.Start(new ProcessStartInfo(_settingsData.EmulatorPath)
                {
                    UseShellExecute = true
                });

                // 等待模拟器启动时间X秒，最大等待搜索设备时间100秒
                await Task.Delay(_settingsData.AutoRunEmulatorWaittingTimeSpan * 1000, token);
                DateTime startTime = DateTime.Now;
                while (!token.IsCancellationRequested)
                {
                    var devices = _maaToolkit.AdbDevice.Find();
                    if (!devices.IsEmpty)
                    {
                        return devices;
                    }
                    if (DateTime.Now > startTime.AddSeconds(MyConstant.AutoSearchEmulatorWaittingTimeSpan))
                    {
                        Utility.PrintError("等待超时，自动打开模拟器失败！");
                        return null;
                    }
                    Thread.Sleep(1000); // 每秒执行一次
                }
            }
            catch (TaskCanceledException)
            {
                Utility.MyDebugWriteLine("手动停止自动打开模拟器");
                //StopCurrentTask();
            }
            catch (Exception e)
            {
                Utility.MyDebugWriteLine($"无法打开模拟器: {e.Message}");
                Utility.PrintError("打开模拟器失败！请检查路径是否正确");
            }
        }
        return null;
    }

    private static bool LoadMaaSource(out MaaResource? resource)
    {
        resource = null;
        //根据配置里的客户端类型选项改变读取的文件路径
        string[] maaSourcePaths = ProgramDataModel.Instance.SettingsData.ClientTypeSettingIndex switch
        {
            (int)EClientTypeSettingOptions.Zh_CN => [MyConstant.MaaSourceDirectory],
            (int)EClientTypeSettingOptions.Zh_CN_Bilibili => [MyConstant.MaaSourceDirectory, MyConstant.MaaSourcePathBiliBiliOverride],
            (int)EClientTypeSettingOptions.Zh_TW => [MyConstant.MaaSourceDirectory, MyConstant.MaaSourcePathZhtwOverride],
            (int)EClientTypeSettingOptions.Jp => [MyConstant.MaaSourceDirectory, MyConstant.MaaSourcePathJpOverride],
            _ => [MyConstant.MaaSourceDirectory],
        };
        try
        {
            resource = new(maaSourcePaths);
        }
        catch (Exception e)
        {
            Utility.PrintLog("加载资源文件失败");
            Utility.MyDebugWriteLine($"Error: {e.Message}");
            return false;
        }
        return true;
    }

    //开启挂机任务
    private async Task StartAfkTask(CancellationToken token)
    {
        bool firstTimeExecute = true;
        DateTime nextCafeRefreshDateTime = GetNextCafeRefreshDateTime(DateTime.Now);

        // 容错：先清除上一个时间段或更早的旧任务
        DateTime previousCafeRefreshDateTime = nextCafeRefreshDateTime.AddHours(-12);
        Application.Current.Dispatcher.Invoke(() =>
        {
            for (int i = 0; i < _waitingTaskChainList.Count; i++)
            {
                if (_waitingTaskChainList[i].ExecuteDateTime < previousCafeRefreshDateTime)
                {
                    _waitingTaskChainList.RemoveAt(i);
                    i--;
                }
            }
        });

        while (!token.IsCancellationRequested)
        {
            if (DateTime.Now >= nextCafeRefreshDateTime)
            {
                // 到达咖啡厅刷新时间，自动生成24小时后的任务队列
                CreateTask(nextCafeRefreshDateTime.AddHours(24));
                nextCafeRefreshDateTime = GetNextCafeRefreshDateTime(DateTime.Now);
            }

            // 判断有无已经到达时间的任务
            foreach (var taskChainItem in _waitingTaskChainList)
            {
                if (DateTime.Now >= taskChainItem.ExecuteDateTime)
                {
                    Utility.MyDebugWriteLine($"检测到有任务可以执行");
                    ProgramDataModel.Instance.IsCurrentTaskExecuting = true;
                    firstTimeExecute = false;
                    CreateCurrentTaskQueue();
                    await ExecuteCurrentTask(token);
                    Utility.MyDebugWriteLine($"已入列的任务执行完成或任务停止");
                    ProgramDataModel.Instance.IsCurrentTaskExecuting = false;
                    break;
                }
            }
            if (firstTimeExecute)
            {
                Utility.PrintLog("暂无可执行任务，小助手待机中");
                firstTimeExecute = false;
            }
            try
            {
                await Task.Delay(1000, token); // 每秒检查一次
            }
            catch (TaskCanceledException)
            {

            }
        }
        StopCurrentTask();
    }

    private async Task ExecuteCurrentTask(CancellationToken token)
    {
        DateTime startDateTime = DateTime.Now;
        while (_currentTaskChainList.Count > 0)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            var currentTaskChain = _currentTaskChainList[0];
            currentTaskChain.Status = ETaskChainStatus.Running;

            if (_maaTasker == null || _maaTasker.IsInvalid)
            {
                Utility.MyDebugWriteLine($"maaTasker is null or invalid!");
                Utility.PrintLog("模拟器未连接");
                if (!await AutoConnect(token))
                {
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        Stop(true);
                    }
                    return;
                }
            }
            if (_maaTasker == null)
            {
                Utility.MyDebugWriteLine("maaTasker依然是null，直接停止任务");
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Stop(true);
                }
                return;
            }
            var device = _maaTasker.Toolkit.AdbDevice.Find();
            if (device.IsEmpty || device.IsInvalid)
            {
                Utility.MyDebugWriteLine($"device is empty or invalid!");
                Utility.PrintLog("模拟器失去连接，正在重新打开...");
                if (!await AutoConnect(token))
                {
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        Stop(true);
                    }
                    return;
                }
            }

            if (token.IsCancellationRequested)
            {
                return;
            }
            Utility.MyDebugWriteLine($"[任务链]   - 执行 - {currentTaskChain.Name}");
            if (currentTaskChain.NeedPrintLog)
            {
                Utility.PrintLog(currentTaskChain.StartLogMessage);
            }
            bool anyTaskFailed = false;
            CurrentTaskChainPrintFinishedLog = true; // 是否打印任务结束的信息(成功、失败、异常)
            foreach (var taskItem in currentTaskChain.TaskQueue)
            {
                // 如果是扫荡困难关卡任务，执行前检测关卡设置是否符合规则
                if (currentTaskChain.TaskChainType == ETaskChainType.SweepHardLevel)
                {
                    var regex = new Regex(@"^(99|[1-9][0-9]?)-[1-3]$");
                    if (!regex.IsMatch(_settingsData.HardLevel))
                    {
                        Utility.PrintError($"关卡设置错误：H{_settingsData.HardLevel}！");
                        CurrentTaskChainPrintFinishedLog = false;
                        break;
                    }
                }
                Utility.MyDebugWriteLine($"[单个任务] - 执行 - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry})");
                Utility.MyDebugWriteLine($"PipelineOverride - {taskItem.PipelineOverride}");
                var status = string.IsNullOrEmpty(taskItem.PipelineOverride) ?
                    _maaTasker.AppendTask(taskItem.Entry).Wait() :
                    _maaTasker.AppendTask(taskItem.Entry, taskItem.PipelineOverride).Wait();
                if (status.IsSucceeded())
                {
                    Utility.MyDebugWriteLine($"[单个任务] - 完成 - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry})");
                }
                else
                {
                    anyTaskFailed = true;
                    Utility.MyDebugWriteLine($"[单个任务] - 失败! - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry}) - {status}");

                    // 检测是否因为停止任务导致释放了maaTasker，如果不break会导致闪退
                    if (token.IsCancellationRequested)
                    {
                        CurrentTaskChainPrintFinishedLog = false;
                        Utility.MyDebugWriteLine("停止任务 - 跳出当前任务链");
                        break;
                    }
                    // 不是手动或系统停止的话,有Pipeline失败后会继续执行后面的Pipeline
                }
            }

            if (anyTaskFailed)
            {
                if (CurrentTaskChainPrintFinishedLog && currentTaskChain.NeedPrintLog)
                    Utility.PrintError(currentTaskChain.FailedLogMessage);
                Utility.MyDebugWriteLine($"[任务链]   - 异常! - 执行任务链过程中出现异常 - {currentTaskChain.Name}");
            }
            else
            {
                if (CurrentTaskChainPrintFinishedLog && currentTaskChain.NeedPrintLog)
                    Utility.PrintLog(currentTaskChain.SucceededLogMessage);
                Utility.MyDebugWriteLine($"[任务链]   - 完成 - {currentTaskChain.Name}");
            }
            //移除任务链。如果是因为用户手动取消的话就不移除，并将状态设为等待中
            if (token.IsCancellationRequested)
            {
                currentTaskChain.Status = ETaskChainStatus.Waiting;
            }
            else
            {
                _currentTaskChainList.Remove(currentTaskChain);
                if (currentTaskChain.NeedShowInWaitingTaskList)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _waitingTaskChainList.Remove(currentTaskChain);
                    });
                }
            }
        }

        RefreshWaitingTaskChainDateTime(DateTime.Now, false);

        bool isExitEmulatorSucceed = false;
        if (_settingsData.IsExitEmulatorAfterTaskFinished)
        {
            if (string.IsNullOrEmpty(_settingsData.EmulatorPath))
            {
                Utility.PrintError("自动退出模拟器失败，请先填写模拟器路径");
            }
            else if (!System.IO.Path.Exists(_settingsData.EmulatorPath))
            {
                Utility.PrintError("自动退出模拟器失败，请检查路径是否正确");
            }
            else
            {
                try
                {
                    if (ExitEmulator())
                    {
                        isExitEmulatorSucceed = true;
                        Utility.PrintLog("已自动退出模拟器");
                    }
                    else
                    {
                        Utility.PrintError("自动退出模拟器失败，请检查路径是否正确");
                    }
                }
                catch (Exception e)
                {
                    Utility.PrintError("自动退出模拟器失败，请检查路径是否正确");
                    Utility.MyDebugWriteLine($"自动退出模拟器时出现错误: {e.Message}");
                }
            }
        }
        if (!isExitEmulatorSucceed && _settingsData.IsExitGameAfterTaskFinished)
        {
            if (_maaTasker != null)
            {
                var status = _maaTasker.AppendTask("CloseGame").Wait();
                if (status.IsSucceeded())
                {
                    Utility.PrintLog("已自动退出游戏");
                }
                else
                {
                    Utility.PrintLog("自动退出游戏失败!");
                }
            }
        }
        Utility.PrintLog("当前任务已完成，小助手待机中");
    }

    private void StopCurrentTask()
    {
        _currentTaskChainList.Clear();
        ProgramDataModel.Instance.IsAfkTaskRunning = false;
        ProgramDataModel.Instance.IsCurrentTaskExecuting = false;
        MainViewModel.Instance.IsStoppingCurrentTask = false;
        _cancellationTokenSource.Dispose();
        Utility.PrintLog("任务已停止");
    }

    // 刷新后续任务时间
    // 规则：只刷新摸头类型任务时间，邀请、应用家具任务不刷新
    public void RefreshWaitingTaskChainDateTime(DateTime dateTime, bool isManualRefresh)
    {
        DateTime nextCafeRefreshDateTime = GetNextCafeRefreshDateTime(dateTime);
        DateTime lastTapTaskChainDateTime = isManualRefresh ? DateTime.MinValue : dateTime;
        // 手动修改任务时间使用另外的处理方法，确保每个摸头任务时间间隔至少为3小时。只处理摸头任务
        for (int i = 0; i < _waitingTaskChainList.Count; i++)
        {
            // 只处理当前时间段的
            if (_waitingTaskChainList[i].ExecuteDateTime < nextCafeRefreshDateTime)
            {
                // 只处理摸头类型任务
                if (_waitingTaskChainList[i].TaskChainType == ETaskChainType.Tap)
                {
                    if (_waitingTaskChainList[i].ExecuteDateTime < lastTapTaskChainDateTime.AddHours(3))
                    {
                        _waitingTaskChainList[i].ExecuteDateTime = lastTapTaskChainDateTime.AddHours(3);
                        // 排除延迟后跨时间段的任务
                        if (_waitingTaskChainList[i].ExecuteDateTime >= nextCafeRefreshDateTime)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _waitingTaskChainList.RemoveAt(i);
                            });
                            i--;
                        }
                    }
                    lastTapTaskChainDateTime = _waitingTaskChainList[i].ExecuteDateTime;
                }
            }
        }
    }

    // 获取下一个服务器刷新的日期时间。读取"客户端类型"设置项来返回不同的时间
    private DateTime GetNextServerRefreshDateTime(DateTime dateTime)
    {
        TimeOnly nextRefreshTimeOnly = _settingsData.ClientTypeSettingIndex switch
        {
            (int)EClientTypeSettingOptions.Zh_CN => MyConstant.RefreshTimeOnlyCN,
            (int)EClientTypeSettingOptions.Zh_CN_Bilibili => MyConstant.RefreshTimeOnlyCN,
            _ => MyConstant.RefreshTimeOnlyNexon,
        };
        DateOnly nextRefreshDateOnly = DateOnly.FromDateTime(dateTime);
        if (TimeOnly.FromDateTime(dateTime) >= nextRefreshTimeOnly)
        {
            nextRefreshDateOnly = nextRefreshDateOnly.AddDays(1);
        }
        return new(nextRefreshDateOnly.Year, nextRefreshDateOnly.Month, nextRefreshDateOnly.Day,
            nextRefreshTimeOnly.Hour, nextRefreshTimeOnly.Minute, nextRefreshTimeOnly.Second);
    }

    // 获取下一个咖啡厅刷新的日期时间
    private DateTime GetNextCafeRefreshDateTime(DateTime dateTime)
    {
        DateTime nextServerRefreshDateTime = GetNextServerRefreshDateTime(dateTime);
        return dateTime < nextServerRefreshDateTime.AddHours(-12) ? nextServerRefreshDateTime.AddHours(-12) : nextServerRefreshDateTime;
    }

    private void AddToWaitingTaskList(TaskChainModel item)
    {
        if (item.NeedShowInWaitingTaskList)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _waitingTaskChainList.Add(item);
            });
        }
    }

    // 生成一个任务片段，该片段中的任务链共用一个执行时间。邀请和应用家具预设的任务链在每个时间段只会生成一次
    private void CreateTaskFragment(DateTime executeDateTime, ref bool _hasCreatedCafe1InviteTask, ref bool _hasCreatedCafe2InviteTask,
        ref bool _hasCreatedCafe1AMApplyLayoutTask, ref bool _hasCreatedCafe1PMApplyLayoutTask,
        ref bool _hasCreatedCafe2AMApplyLayoutTask, ref bool _hasCreatedCafe2PMApplyLayoutTask,
        ref bool _hasCreatedSweepHardLevelTask)
    {
        DateTime nextServerRefreshDateTime = GetNextServerRefreshDateTime(executeDateTime); //04:00 or 03:00
        DateTime pmRefreshDateTime = nextServerRefreshDateTime.AddHours(-12); //16:00
        Queue<TaskModel> tempQueue = new();

        //生成摸头任务
        tempQueue.Enqueue(new("进入1号咖啡厅", "MoveToCafe1", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("(1号)咖啡厅摸头", "CafeTap", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("(2号)咖啡厅摸头", "CafeTap", string.Empty, ETaskType.Normal));
        AddToWaitingTaskList(new("咖啡厅摸头", executeDateTime, ETaskChainType.Tap, true, true, string.Empty, tempQueue));

        //生成2号咖啡厅邀请任务
        if (!_hasCreatedCafe2InviteTask && _settingsData.Cafe2InviteTimeSettingIndex != (int)ECafeInviteTimeSettingOptions.DoNotUse)
        {
            DateTime inviteTime = GetInviteTaskDateTime(_settingsData.Cafe2InviteTimeSettingIndex, executeDateTime);
            if (inviteTime != DateTime.MaxValue && inviteTime == executeDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("2号咖啡厅邀请+摸头", "CafeInvite", string.Empty, ETaskType.Cafe2Invite));
                AddToWaitingTaskList(new("2号咖啡厅邀请", inviteTime, ETaskChainType.Invite, true, true, string.Empty, tempQueue));
                _hasCreatedCafe2InviteTask = true;
            }
        }

        //生成2号咖啡厅应用家具预设任务
        if (_settingsData.IsCafe2EnableApplyLayout)
        {
            if (_hasCreatedCafe2AMApplyLayoutTask == false && executeDateTime < pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("2号咖啡厅应用家具预设AM", "CafeApplyLayout", string.Empty, ETaskType.Cafe2AMApplyLayout));
                AddToWaitingTaskList(new("2号咖啡厅应用家具预设", executeDateTime, ETaskChainType.ApplyLayout, true, true, string.Empty, tempQueue));
                _hasCreatedCafe2AMApplyLayoutTask = true;
            }
            if (_hasCreatedCafe2PMApplyLayoutTask == false && executeDateTime >= pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("2号咖啡厅应用家具预设PM", "CafeApplyLayout", string.Empty, ETaskType.Cafe2PMApplyLayout));
                AddToWaitingTaskList(new("2号咖啡厅应用家具预设", executeDateTime, ETaskChainType.ApplyLayout, true, true, string.Empty, tempQueue));
                _hasCreatedCafe2PMApplyLayoutTask = true;
            }
        }

        //生成1号咖啡厅邀请任务
        if (!_hasCreatedCafe1InviteTask && _settingsData.Cafe1InviteTimeSettingIndex != (int)ECafeInviteTimeSettingOptions.DoNotUse)
        {
            DateTime inviteTime = GetInviteTaskDateTime(_settingsData.Cafe1InviteTimeSettingIndex, executeDateTime);
            if (inviteTime != DateTime.MaxValue && inviteTime == executeDateTime)
            {

                tempQueue = new();
                tempQueue.Enqueue(new("进入1号咖啡厅", "MoveToCafe1", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("1号咖啡厅邀请+摸头", "CafeInvite", string.Empty, ETaskType.Cafe1Invite));
                AddToWaitingTaskList(new("1号咖啡厅邀请", inviteTime, ETaskChainType.Invite, true, true, string.Empty, tempQueue));
                _hasCreatedCafe1InviteTask = true;
            }
        }

        //生成1号咖啡厅应用家具预设任务
        if (_settingsData.IsCafe1EnableApplyLayout)
        {
            if (_hasCreatedCafe1AMApplyLayoutTask == false && executeDateTime < pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入1号咖啡厅", "MoveToCafe1", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("1号咖啡厅应用家具预设", "CafeApplyLayout", string.Empty, ETaskType.Cafe1AMApplyLayout));
                AddToWaitingTaskList(new("1号咖啡厅应用家具预设", executeDateTime, ETaskChainType.ApplyLayout, true, true, string.Empty, tempQueue));
                _hasCreatedCafe1AMApplyLayoutTask = true;
            }
            if (_hasCreatedCafe1PMApplyLayoutTask == false && executeDateTime >= pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入1号咖啡厅", "MoveToCafe1", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("1号咖啡厅应用家具预设", "CafeApplyLayout", string.Empty, ETaskType.Cafe1PMApplyLayout));
                AddToWaitingTaskList(new("1号咖啡厅应用家具预设", executeDateTime, ETaskChainType.ApplyLayout, true, true, string.Empty, tempQueue));
                _hasCreatedCafe1PMApplyLayoutTask = true;
            }
        }

        //生成扫荡困难关卡任务，只在上午时间段生成
        if (_settingsData.IsCreateSweepHardLevelTask)
        {
            if (_hasCreatedSweepHardLevelTask == false && executeDateTime < pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new($"扫荡关卡H{_settingsData.HardLevel}", "SweepHardLevel", string.Empty, ETaskType.SweepHardLevel));
                AddToWaitingTaskList(new($"扫荡困难关卡", 
                    executeDateTime, ETaskChainType.SweepHardLevel, true, true, string.Empty, tempQueue));
                _hasCreatedSweepHardLevelTask = true;
            }
        }
    }

    /// <summary>获取邀请任务的时间</summary>
    /// <returns>返回MaxValue则表示不生成</returns>
    private DateTime GetInviteTaskDateTime(int _settingIndex, DateTime currentDateTime)
    {
        /// 生成邀请任务规则:
        /// 选择AM： 4:00-15:59之间 立即, 16:00-次日3:59之间 不生成
        /// 选择PM： 4:00-15:59之间 不生成, 16:00-次日3:59之间 立即
        /// To-do特殊处理：距离刷新只剩3分钟的时候不生成任何任务，避免使用邀请券的时候刚好到了刷新时间
        DateTime nextServerRefreshDateTime = GetNextServerRefreshDateTime(currentDateTime); //04:00 or 03:00
        DateTime pmRefreshDateTime = nextServerRefreshDateTime.AddHours(-12); //16:00 or 15:00
        switch (_settingIndex)
        {
            case (int)ECafeInviteTimeSettingOptions.AM:
                if (currentDateTime < pmRefreshDateTime)
                    return currentDateTime;
                else
                    return DateTime.MaxValue;
            case (int)ECafeInviteTimeSettingOptions.PM:
                if (currentDateTime >= pmRefreshDateTime)
                    return currentDateTime;
                else
                    return DateTime.MaxValue;
            case (int)ECafeInviteTimeSettingOptions.Immediately:
                return currentDateTime;
            default:
                return DateTime.MaxValue;
        }
    }

    // 把已经到达时间的任务入列。同时在头部添加一个启动游戏的任务，在尾部添加一个返回主界面的任务。
    private void CreateCurrentTaskQueue()
    {
        // 如果第一个是重启游戏任务，那么就不用再添加启动游戏任务了
        if (_waitingTaskChainList[0].TaskQueue.Peek().Type == ETaskType.RestartGame)
        {
            if (ProgramDataModel.Instance.SettingsData.IsAutoUpdateResources)
            {
                UpdateTool.CheckNewVersion(false, true, out _, out _, false);
                // 等待2秒，用于覆盖更新资源文件
                Task.Delay(2000).Wait();
                if (!LoadMaaSource(out MaaResource? maaResource) || maaResource == null)
                {
                    Utility.MyDebugWriteLine("更新资源文件后，重新加载失败！");
                }
                else
                {
                    if (_maaTasker != null && !_maaTasker.IsInvalid)
                    {
                        _maaTasker = new()
                        {

                            Controller = _maaTasker.Controller,
                            Resource = maaResource,
                            DisposeOptions = DisposeOptions.All,
                        };
                    }
                    else
                    {
                        Utility.MyDebugWriteLine("_maaTasker is null!");
                    }
                }
                
            }
        }
        else
        {
            Queue<TaskModel> tempQueue0 = new();
            tempQueue0.Enqueue(new("启动游戏", "StartGame", GetOverrideJsonWithReadConfig(ETaskType.StartGame), ETaskType.StartGame));
            _currentTaskChainList.Add(new("启动游戏", DateTime.Now, ETaskChainType.System, false, true, string.Empty, tempQueue0));
        }

        foreach (var taskChainItem in _waitingTaskChainList)
        {
            // 判断有无已经到达时间的任务
            if (DateTime.Now >= taskChainItem.ExecuteDateTime)
            {
                taskChainItem.Status = ETaskChainStatus.InCurrentQueue;
                // 读取设置(进入执行中队列时候才会读取)
                foreach (var taskItem in taskChainItem.TaskQueue)
                {
                    taskItem.PipelineOverride = GetOverrideJsonWithReadConfig(taskItem.Type);
                }
                // 扫荡困难关卡的任务，开始的Log特别处理，显示关卡
                if (taskChainItem.TaskChainType == ETaskChainType.SweepHardLevel)
                {
                    taskChainItem.OverrideStartLogMessage = $"开始任务：扫荡关卡H{_settingsData.HardLevel}";
                }
                _currentTaskChainList.Add(taskChainItem);
            }
        }

        Queue<TaskModel> tempQueue = new();
        tempQueue.Enqueue(new("返回主界面", "RefreshGame@BackToHomePage", string.Empty, ETaskType.Normal));
        _currentTaskChainList.Add(new("返回主界面", DateTime.Now, ETaskChainType.System, false, false, string.Empty, tempQueue));
        foreach (var item in _currentTaskChainList)
        {
            Utility.MyDebugWriteLine($"{item.ExecuteDateTime} - {item.Name}");
        }
    }

    // 读取设置来生成所需的pipeline override json
    private string GetOverrideJsonWithReadConfig(ETaskType type)
    {
        string overrideJson;
        string overrideSortType;
        string overrideSortOrder;
        switch (type)
        {
            case ETaskType.Normal:
                return "";
            //启动游戏
            case ETaskType.StartGame:
                return _settingsData.IsReconnectAfterDuplicatedLogin ?
                    string.Empty : "{\"RefreshGame@DuplicatedLoginPopUp\":{\"next\":\"RefreshGame@DuplicatedLoginStopTask\"}}";
            //1号咖啡厅邀请
            case ETaskType.Cafe1Invite:
                overrideSortType = string.Empty;
                overrideSortOrder = string.Empty;
                switch (_settingsData.ClientTypeSettingIndex)
                {
                    default:
                        switch (_settingsData.Cafe1InviteSortTypeSettingIndex)
                        {
                            case (int)ECafeInviteSortTypeSettingOptions.BondLvFromLowToHigh:
                                overrideSortType = "好感等级";
                                overrideSortOrder = "SortLowToHigh.png";
                                break;
                            case (int)ECafeInviteSortTypeSettingOptions.Pinned:
                                overrideSortType = "精选";
                                overrideSortOrder = "SortHighToLow.png";
                                break;
                        }
                        break;
                    case (int)EClientTypeSettingOptions.Zh_TW:
                        switch (_settingsData.Cafe1InviteSortTypeSettingIndex)
                        {
                            case (int)ECafeInviteSortTypeSettingOptions.BondLvFromLowToHigh:
                                overrideSortType = "羈絆等級";
                                overrideSortOrder = "SortLowToHigh.png";
                                break;
                            case (int)ECafeInviteSortTypeSettingOptions.Pinned:
                                overrideSortType = "精選";
                                overrideSortOrder = "SortHighToLow.png";
                                break;
                        }
                        break;
                    case (int)EClientTypeSettingOptions.Jp:
                        switch (_settingsData.Cafe1InviteSortTypeSettingIndex)
                        {
                            case (int)ECafeInviteSortTypeSettingOptions.BondLvFromLowToHigh:
                                overrideSortType = "絆ランク";
                                overrideSortOrder = "SortLowToHigh.png";
                                break;
                            case (int)ECafeInviteSortTypeSettingOptions.Pinned:
                                overrideSortType = "お気に入";
                                overrideSortOrder = "SortHighToLow.png";
                                break;
                        }
                        break;
                }
                overrideJson = "{";
                if (!string.IsNullOrEmpty(overrideSortType))
                    overrideJson += "\"CafeInvite@ChangeSortType\":{\"text\":\"" + overrideSortType + "\"},";
                if (!string.IsNullOrEmpty(overrideSortOrder))
                    overrideJson += "\"CafeInvite@RecognizeSortOrder\":{\"template\":\"" + overrideSortOrder + "\"},";
                if (_settingsData.Cafe1InviteIndexSettingIndex != 0)
                    overrideJson += "\"CafeInvite@Invite\":{\"index\":" + _settingsData.Cafe1InviteIndexSettingIndex + "},";
                if (!_settingsData.IsCafe1AllowInviteNeighboring)
                    overrideJson += "\"CafeInvite@InviteConfirmPopupNeighboring\":{\"next\":\"CafeInvite@InviteCancel\"},";
                if (!_settingsData.IsCafe1AllowInviteNeighboringSwapAlt)
                    overrideJson += "\"CafeInvite@InviteConfirmPopupNeighboringSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"},";
                if (!_settingsData.IsCafe1AllowInviteSwapAlt)
                    overrideJson += "\"CafeInvite@InviteConfirmPopupSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"},";
                if (overrideJson.EndsWith(','))
                    overrideJson = overrideJson.Substring(0, overrideJson.Length - 1);
                overrideJson += "}";
                return overrideJson;
            //1号咖啡厅应用家具预设
            case ETaskType.Cafe1AMApplyLayout:
                return "{\"CafeLayout@ApplyLayout\":{\"index\":" + _settingsData.Cafe1AMApplyLayoutIndex + "}}";
            case ETaskType.Cafe1PMApplyLayout:
                return "{\"CafeLayout@ApplyLayout\":{\"index\":" + _settingsData.Cafe1PMApplyLayoutIndex + "}}";
            //2号咖啡厅邀请
            case ETaskType.Cafe2Invite:
                overrideSortType = string.Empty;
                overrideSortOrder = string.Empty;
                switch (_settingsData.ClientTypeSettingIndex)
                {
                    default:
                        switch (_settingsData.Cafe2InviteSortTypeSettingIndex)
                        {
                            case (int)ECafeInviteSortTypeSettingOptions.BondLvFromLowToHigh:
                                overrideSortType = "好感等级";
                                overrideSortOrder = "SortLowToHigh.png";
                                break;
                            case (int)ECafeInviteSortTypeSettingOptions.Pinned:
                                overrideSortType = "精选";
                                overrideSortOrder = "SortHighToLow.png";
                                break;
                        }
                        break;
                    case (int)EClientTypeSettingOptions.Zh_TW:
                        switch (_settingsData.Cafe2InviteSortTypeSettingIndex)
                        {
                            case (int)ECafeInviteSortTypeSettingOptions.BondLvFromLowToHigh:
                                overrideSortType = "羈絆等級";
                                overrideSortOrder = "SortLowToHigh.png";
                                break;
                            case (int)ECafeInviteSortTypeSettingOptions.Pinned:
                                overrideSortType = "精選";
                                overrideSortOrder = "SortHighToLow.png";
                                break;
                        }
                        break;
                    case (int)EClientTypeSettingOptions.Jp:
                        switch (_settingsData.Cafe2InviteSortTypeSettingIndex)
                        {
                            case (int)ECafeInviteSortTypeSettingOptions.BondLvFromLowToHigh:
                                overrideSortType = "絆ランク";
                                overrideSortOrder = "SortLowToHigh.png";
                                break;
                            case (int)ECafeInviteSortTypeSettingOptions.Pinned:
                                overrideSortType = "お気に入";
                                overrideSortOrder = "SortHighToLow.png";
                                break;
                        }
                        break;
                }
                overrideJson = "{";
                if (!string.IsNullOrEmpty(overrideSortType))
                    overrideJson += "\"CafeInvite@ChangeSortType\":{\"text\":\"" + overrideSortType + "\"},";
                if (!string.IsNullOrEmpty(overrideSortOrder))
                    overrideJson += "\"CafeInvite@RecognizeSortOrder\":{\"template\":\"" + overrideSortOrder + "\"},";
                if (_settingsData.Cafe2InviteIndexSettingIndex != 0)
                    overrideJson += "\"CafeInvite@Invite\":{\"index\":" + _settingsData.Cafe2InviteIndexSettingIndex + "},";
                if (!_settingsData.IsCafe2AllowInviteNeighboring)
                    overrideJson += "\"CafeInvite@InviteConfirmPopupNeighboring\":{\"next\":\"CafeInvite@InviteCancel\"},";
                if (!_settingsData.IsCafe2AllowInviteNeighboringSwapAlt)
                    overrideJson += "\"CafeInvite@InviteConfirmPopupNeighboringSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"},";
                if (!_settingsData.IsCafe2AllowInviteSwapAlt)
                    overrideJson += "\"CafeInvite@InviteConfirmPopupSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"},";
                if (overrideJson.EndsWith(','))
                    overrideJson = overrideJson.Substring(0, overrideJson.Length - 1);
                overrideJson += "}";
                return overrideJson;
            //2号咖啡厅应用家具预设
            case ETaskType.Cafe2AMApplyLayout:
                return "{\"CafeLayout@ApplyLayout\":{\"index\":" + _settingsData.Cafe2AMApplyLayoutIndex + "}}";
            case ETaskType.Cafe2PMApplyLayout:
                return "{\"CafeLayout@ApplyLayout\":{\"index\":" + _settingsData.Cafe2PMApplyLayoutIndex + "}}";
            //扫荡困难关卡
            case ETaskType.SweepHardLevel:
                return "{\"Mission@RecognizeMissionLevel\":{\"text\":\"" + _settingsData.HardLevel + "\"}}";
            default:
                return "";
        }
    }

    public void MaaTaskerDispose()
    {
        try
        {
            if (_maaTasker != null)
            {
                _maaTasker.Stop().Wait();
                _maaTasker.Dispose();
            }
        }
        catch (Exception ex)
        {
            Utility.MyDebugWriteLine(ex.Message);
        }
    }

    /// <summary>
    /// 退出模拟器，针对MuMu模拟器、雷电模拟器做特殊处理，调用命令行来关闭
    /// MuMu模拟器如果直接关闭进程，虚拟机并不会顺带关闭，导致资源占用
    /// </summary>
    private bool ExitEmulator()
    {
        string mumu = "MuMuPlayer";
        string leidian = "dnplayer";
        if (_settingsData.EmulatorPath.IndexOf(mumu, StringComparison.OrdinalIgnoreCase) >= 0
            || _settingsData.EmulatorPath.IndexOf(leidian, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // 调用MuMu的控制台方法关闭模拟器
            string? directory = System.IO.Path.GetDirectoryName(_settingsData.EmulatorPath);
            if (string.IsNullOrEmpty(directory))
            { 
                return false;
            }
            string programPath = string.Empty;
            string argument = string.Empty;
            // MuMu模拟器
            if (_settingsData.EmulatorPath.IndexOf(mumu, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                programPath = System.IO.Path.Combine(directory, "MuMuManager.exe");
                argument = $"control -v {_settingsData.ExitEmulatorIndex} shutdown";
            }
            // 雷电模拟器
            else if (_settingsData.EmulatorPath.IndexOf(leidian, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                programPath = System.IO.Path.Combine(directory, "ldconsole.exe");
                argument = $"quit --index {_settingsData.ExitEmulatorIndex}";
            }

            // 执行命令行任务
            Utility.MyDebugWriteLine($"执行命令行 - ProgramPath:{programPath} - Argument:{argument}");
            ProcessStartInfo processStartInfo = new()
            {
                FileName = programPath,
                Arguments = argument,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new();
            process.StartInfo = processStartInfo;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // 检查是否有错误输出
            if (!string.IsNullOrEmpty(errorOutput))
            {
                Utility.MyDebugWriteLine("执行命令行遇到错误！运行结果：" + output);
                Utility.MyDebugWriteLine("错误：" + errorOutput);
                return false;
            }
            return true;
        }
        // 通用的退出方法，直接关闭同名进程
        else
        {
            string fileName = System.IO.Path.GetFileName(_settingsData.EmulatorPath);
            Process[] processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(fileName));
            foreach (Process process in processes)
            {
                if (process.MainModule != null &&
                    process.MainModule.FileName.Equals(_settingsData.EmulatorPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    process.Kill();
                    process.WaitForExit();
                    return true;
                }
            }
        }

        return false;
    }

    // 从json里读取配置并使用配置初始化
    [Obsolete]
    private static MaaAdbController InitializeController()
    {
        //To-do 加上从配置里读取?
        string adbPath = "C:/Program Files/MuMuPlayer-12.0/shell/adb.exe";
        string adbSerial = "127.0.0.1:16384";
        AdbScreencapMethods adbScreencapMethods = AdbScreencapMethods.Default;
        AdbInputMethods inputMethods = AdbInputMethods.Default;
        string config = "{\"extras\":{\"mumu\":{\"enable\":true,\"index\":0,\"path\":\"C:/Program Files/MuMuPlayer-12.0\"}}";

        MaaAdbController controller = new(adbPath, adbSerial, adbScreencapMethods, inputMethods, config);
        return controller;
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