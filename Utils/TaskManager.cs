using MaaBATapAssistant.ViewModels;
using MaaBATapAssistant.Models;
using MaaFramework.Binding;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading;
using System.Diagnostics;

namespace MaaBATapAssistant.Utils;

public class TaskManager
{
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
    private readonly List<TaskChainModel> _currentTaskChainList = new();
    private readonly SettingsData _settingsData = ProgramDataModel.Instance.SettingsData;
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
            MainViewModel.Instance.PrintLog("暂无任务，请先生成任务");
            return;
        }
        ProgramDataModel.Instance.IsAfkTaskRunning = true;
        _cancellationTokenSource = new();
        await Task.Run(async () =>
        {
            if (!AutoConnect())
            {
                ProgramDataModel.Instance.IsAfkTaskRunning = false;
                ProgramDataModel.Instance.IsCurrentTaskExecuting = false;
                return;
            }
            PreventSleepTool.PreventSleep();
            await StartAfkTask(_cancellationTokenSource.Token);
        });
    }

    public async void Stop()
    {
        MainViewModel.Instance.PrintLog("正在停止...");
        MainViewModel.Instance.IsStoppingCurrentTask = true;
        PreventSleepTool.RestoreSleep();
        _cancellationTokenSource.Cancel();
        await Task.Run(() =>
        {
            if (_maaTasker != null)
            {
                _maaTasker.Abort().Wait();
                _maaTasker.Dispose();
            }
        });
    }

    public void CreateTask()
    {
        //Test();
        
        _waitingTaskChainList.Clear();

        DateTime taskDateTime = DateTime.Now;
        DateTime nextRefreshDateTime = GetNextRefreshDateTime(DateTime.Now); //04:00 or 03:00
        DateTime pmRefreshTime = nextRefreshDateTime.AddHours(-12); //16:00 or 15:00
        bool hasCreatedCafe1InviteTask = false;
        bool hasCreatedCafe2InviteTask = false;
        bool hasCreatedCafe1AMApplyLayoutTask = false;
        bool hasCreatedCafe1PMApplyLayoutTask = false;
        bool hasCreatedCafe2AMApplyLayoutTask = false;
        bool hasCreatedCafe2PMApplyLayoutTask = false;
        bool hasResetTimeToPMRefresh = taskDateTime >= pmRefreshTime;
        //循环生成当日任务
        while (taskDateTime < nextRefreshDateTime)
        {
            if (!hasResetTimeToPMRefresh && taskDateTime >= pmRefreshTime)
            {
                taskDateTime = pmRefreshTime;
                hasResetTimeToPMRefresh = true;
            }
            CreateTaskFragment(taskDateTime, ref hasCreatedCafe1InviteTask, ref hasCreatedCafe2InviteTask, ref hasCreatedCafe1AMApplyLayoutTask,
                ref hasCreatedCafe1PMApplyLayoutTask, ref hasCreatedCafe2AMApplyLayoutTask, ref hasCreatedCafe2PMApplyLayoutTask);
            taskDateTime = taskDateTime.AddHours(3);
        }
        //最后添加一个重启任务，只做展示作用，实际的重启在挂机任务里
        Queue<TaskModel> emptyQueue = new();
        AddToWaitingTaskList(new("重启游戏", nextRefreshDateTime, true, false, emptyQueue));

        //对任务按时间排序?
        
    }

    //自动连接模拟器和Maa资源，选择搜索到的第一个模拟器
    private bool AutoConnect()
    {
        var devices = _maaToolkit.AdbDevice.Find();
        if (devices.Count <= 0)
        {
            MainViewModel.Instance.PrintLog("找不到模拟器，请先手动打开模拟器");
            return false;
        }

        MaaResource maaSourcePath = new();
        if (LoadMaaSource(ref maaSourcePath) is false)
            return false;

        if (_maaTasker is not null)
            if (_maaTasker.Initialized)
            {
                MainViewModel.Instance.PrintLog("已连接至模拟器" + devices[0].Name);
                return true;
            }

        MainViewModel.Instance.PrintLog("正在连接模拟器...");
        try
        {
            _maaTasker = new()
            {
                //默认使用第0个设备，后续再改
                Controller = devices[0].ToAdbController(),
                Resource = maaSourcePath,
                DisposeOptions = DisposeOptions.All,
            };
        }
        catch (Exception)
        {
            MainViewModel.Instance.PrintLog("任务初始化失败，可能是ADB连接出现问题，请尝试重启模拟器或者重启系统");
            return false;
        }
        if (_maaTasker is null || _maaTasker.Initialized is false)
        {
            MainViewModel.Instance.PrintLog("任务初始化失败");
            return false;
            throw new InvalidOperationException();
        }
        MainViewModel.Instance.PrintLog("成功连接至模拟器" + devices[0].Name);
        return true;
    }

    private static bool LoadMaaSource(ref MaaResource _resource)
    {
        //根据配置里的客户端类型选项改变读取的文件路径
        string[] maaSourcePath = ProgramDataModel.Instance.SettingsData.ClientTypeSettingIndex switch
        {
            (int)EClientTypeSettingOptions.Zh_CN => [MyConstant.MaaSourcePath],
            (int)EClientTypeSettingOptions.Zh_CN_Bilibili => [MyConstant.MaaSourcePath, MyConstant.MaaSourcePathBiliBiliOverride],
            _ => [MyConstant.MaaSourcePath],
        };
        try
        {
            _resource = new(maaSourcePath);
        }
        catch (Exception)
        {
            MainViewModel.Instance.PrintLog("加载资源文件失败");
            return false;
        }
        _resource = new(maaSourcePath);
        return true;
    }

    //开启挂机任务
    private async Task StartAfkTask(CancellationToken token)
    {
        bool firstTimeExecute = true;
        DateTime nextRefreshDateTime = GetNextRefreshDateTime(DateTime.Now);

        while (!token.IsCancellationRequested)
        {
            if (DateTime.Now >= nextRefreshDateTime)
            {
                MainViewModel.Instance.PrintLog("日期已变更，重启游戏中...");
                //到达服务器刷新时间，直接关闭游戏，同时重新生成任务队列，再继续执行新的任务队列
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    Debug.WriteLine(DateTime.Now + "  日期已变更，重启游戏中..."); //，将返回标题画面...
                    _maaTasker?.AppendPipeline(MyConstant.CloseBAPipelineEntry).Wait();
                    await Task.Delay(3000);
                    _waitingTaskChainList.Clear();
                    GC.Collect();
                    CreateTask();
                });
                nextRefreshDateTime = GetNextRefreshDateTime(DateTime.Now);
            }

            var item = _waitingTaskChainList[0];
            if (item != null && DateTime.Now >= item.ExecuteDateTime)
            {
                Debug.WriteLine(DateTime.Now + "  检测到有任务可以执行");
                ProgramDataModel.Instance.IsCurrentTaskExecuting = true;
                firstTimeExecute = false;
                CreateCurrentTaskQueue();
                ExecuteCurrentTask(_cancellationTokenSource.Token);
                Debug.WriteLine(DateTime.Now + "  已入列的任务执行完成或者已停止");
                ProgramDataModel.Instance.IsCurrentTaskExecuting = false;
            }
            else if(firstTimeExecute)
            {
                MainViewModel.Instance.PrintLog("暂无可执行任务，小助手待机中");
                firstTimeExecute = false;
            }

            await Task.Delay(1000); // 每秒检查一次
        }

        StopCurrentTask();
    }

    private void ExecuteCurrentTask(CancellationToken token)
    {
        while (_currentTaskChainList.Count > 0)
        {
            if (token.IsCancellationRequested)
            {
                //StopCurrentTask();
                return;
            }
            var currentTaskChain = _currentTaskChainList[0];
            currentTaskChain.Status = ETaskChainStatus.Running;

            Debug.WriteLine($"{DateTime.Now}   准备执行任务链：{currentTaskChain.Name}");
            bool anyTaskFailed = false;
            if (currentTaskChain.NeedPrintLog)
            {
                MainViewModel.Instance.PrintLog(currentTaskChain.StartLogMessage);
            }
            foreach (var taskItem in currentTaskChain.TaskQueue)
            {
                Debug.WriteLine($"{DateTime.Now}   准备执行单个任务 - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry})");
                if (_maaTasker is null)
                {
                    Debug.WriteLine($"{DateTime.Now}   maaTasker is null!");
                    return;
                }
                var status = taskItem.PipelineOverride == "" ?
                    _maaTasker.AppendPipeline(taskItem.Entry).Wait() :
                    _maaTasker.AppendPipeline(taskItem.Entry, taskItem.PipelineOverride).Wait();
                if (status.IsSucceeded())
                {
                    Debug.WriteLine($"{DateTime.Now}   执行单个任务完成 - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry})");
                }
                else
                {
                    if (currentTaskChain.NeedPrintLog)
                        MainViewModel.Instance.PrintLog(currentTaskChain.FailedLogMessage);
                    anyTaskFailed = true;
                    Debug.WriteLine($"{DateTime.Now}   执行单个任务失败 - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry}) - {status}");
                    break;
                }
            }
                
            if (currentTaskChain.NeedPrintLog && anyTaskFailed is false)
            {
                MainViewModel.Instance.PrintLog(currentTaskChain.SucceededLogMessage);
            }
            Debug.WriteLine($"{DateTime.Now}   执行任务链完成：{currentTaskChain.Name}");
            
            //移除任务链
            _currentTaskChainList.Remove(currentTaskChain);
            if (currentTaskChain.NeedShowInWaitingTaskList)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _waitingTaskChainList.Remove(currentTaskChain);
                });
            }
        }

        //刷新后续任务时间
        DateTime endTime = DateTime.Now;
        DateTime nextRefreshDateTime = GetNextRefreshDateTime(endTime);
        DateTime pmRefreshDateTime = nextRefreshDateTime.AddHours(-12);
        TimeSpan timeSpan = endTime.AddHours(3) - _waitingTaskChainList[0].ExecuteDateTime; //摸头cd固定3小时
        Application.Current.Dispatcher.Invoke(() =>
        {
            for (int i = 0; i < _waitingTaskChainList.Count-1; i++)
            {
                if (endTime < pmRefreshDateTime)
                {
                    if (_waitingTaskChainList[i].ExecuteDateTime < pmRefreshDateTime)
                    {
                        _waitingTaskChainList[i].ExecuteDateTime += timeSpan;
                        if (_waitingTaskChainList[i].ExecuteDateTime >= pmRefreshDateTime)
                        {
                            //排除延迟后跨时间段的任务
                            _waitingTaskChainList.RemoveAt(i);
                            i--;
                        }
                    }
                }
                else
                {
                    _waitingTaskChainList[i].ExecuteDateTime += timeSpan;
                    if (_waitingTaskChainList[i].ExecuteDateTime >= nextRefreshDateTime)
                    {
                        _waitingTaskChainList.RemoveAt(i);
                        i--;
                    }
                }
            }
        });

        MainViewModel.Instance.PrintLog("当前任务已完成，小助手待机中");
    }

    private void StopCurrentTask()
    {
        _currentTaskChainList.Clear();
        ProgramDataModel.Instance.IsAfkTaskRunning = false;
        ProgramDataModel.Instance.IsCurrentTaskExecuting = false;
        MainViewModel.Instance.IsStoppingCurrentTask = false;
        MainViewModel.Instance.PrintLog("已停止");
    }
    
    //获取下一个服务器刷新的日期时间。读取"客户端类型"设置项来返回不同的时间
    private DateTime GetNextRefreshDateTime(DateTime dateTime)
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

    //生成一个任务片段，该片段中的任务链共用一个执行时间。邀请和应用家具预设的任务链在每个时间段只会生成一次
    private void CreateTaskFragment(DateTime executeDateTime, ref bool _hasCreatedCafe1InviteTask, ref bool _hasCreatedCafe2InviteTask,
        ref bool _hasCreatedCafe1AMApplyLayoutTask, ref bool _hasCreatedCafe1PMApplyLayoutTask, 
        ref bool _hasCreatedCafe2AMApplyLayoutTask, ref bool _hasCreatedCafe2PMApplyLayoutTask)
    {
        DateTime nextRefreshDateTime = GetNextRefreshDateTime(DateTime.Now); //04:00 or 03:00
        DateTime pmRefreshDateTime = nextRefreshDateTime.AddHours(-12); //16:00
        Queue<TaskModel> tempQueue = new();
        
        //生成摸头任务
        tempQueue.Enqueue(new("进入1号咖啡厅", "MoveToCafe1", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("(1号)咖啡厅摸头", "CafeTap", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("(2号)咖啡厅摸头", "CafeTap", string.Empty, ETaskType.Normal));
        AddToWaitingTaskList(new("咖啡厅摸头", executeDateTime, true, true, tempQueue));
        
        //生成2号咖啡厅邀请任务
        if (!_hasCreatedCafe2InviteTask && _settingsData.Cafe2InviteTimeSettingIndex != (int)ECafeInviteTimeSettingOptions.DoNotUse)
        {
            DateTime inviteTime = GetInviteTaskDateTime(_settingsData.Cafe2InviteTimeSettingIndex, executeDateTime);
            if (inviteTime != DateTime.MaxValue && inviteTime == executeDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("2号咖啡厅邀请", "CafeInvite", string.Empty, ETaskType.Cafe2Invite));
                tempQueue.Enqueue(new("(2号)咖啡厅摸头", "CafeTap", string.Empty, ETaskType.Normal));
                AddToWaitingTaskList(new("2号咖啡厅邀请", inviteTime, true, true, tempQueue));
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
                AddToWaitingTaskList(new("2号咖啡厅应用家具预设", executeDateTime, true, true, tempQueue));
                _hasCreatedCafe2AMApplyLayoutTask = true;
            }
            if (_hasCreatedCafe2PMApplyLayoutTask == false && executeDateTime >= pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("2号咖啡厅应用家具预设PM", "CafeApplyLayout", string.Empty, ETaskType.Cafe2PMApplyLayout));
                AddToWaitingTaskList(new("2号咖啡厅应用家具预设", executeDateTime, true, true, tempQueue));
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
                tempQueue.Enqueue(new("1号咖啡厅邀请", "CafeInvite", string.Empty, ETaskType.Cafe1Invite));
                tempQueue.Enqueue(new("(1号)咖啡厅摸头", "CafeTap", string.Empty, ETaskType.Normal));
                AddToWaitingTaskList(new("1号咖啡厅邀请", inviteTime, true, true, tempQueue));
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
                AddToWaitingTaskList(new("1号咖啡厅应用家具预设", executeDateTime, true, true, tempQueue));
                _hasCreatedCafe1AMApplyLayoutTask = true;
            }
            if (_hasCreatedCafe1PMApplyLayoutTask == false && executeDateTime >= pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入1号咖啡厅", "MoveToCafe1", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("1号咖啡厅应用家具预设", "CafeApplyLayout", string.Empty, ETaskType.Cafe1PMApplyLayout));
                AddToWaitingTaskList(new("1号咖啡厅应用家具预设", executeDateTime, true, true, tempQueue));
                _hasCreatedCafe1PMApplyLayoutTask = true;
            }
        }
    }

    private DateTime GetInviteTaskDateTime(int _settingIndex, DateTime currentDateTime)
    {
        ///返回MaxValue则表示不生成
        ///生成邀请任务规则:选择AM：4:00-15:55:00之间 立即; 选择PM：16:00-次日3:59之间 立即,4:00-16:00之间 16:00
        ///特殊处理：距离刷新只剩3分钟的时候不生成任何任务，避免使用邀请券的时候刚好到了刷新时间
        DateTime nextRefreshDateTime = GetNextRefreshDateTime(DateTime.Now); //04:00 or 03:00
        DateTime lastRefreshDateTime = nextRefreshDateTime.AddHours(-24); //04:00
        DateTime pmRefreshDateTime = nextRefreshDateTime.AddHours(-12); //16:00

        switch (_settingIndex)
        {
            case (int)ECafeInviteTimeSettingOptions.AM:
                if (currentDateTime >= lastRefreshDateTime && currentDateTime < pmRefreshDateTime)
                    return currentDateTime;
                else
                    return DateTime.MaxValue;
            case (int)ECafeInviteTimeSettingOptions.PM:
                if (currentDateTime >= pmRefreshDateTime && currentDateTime < nextRefreshDateTime)
                    return currentDateTime;
                else if (currentDateTime >= lastRefreshDateTime && currentDateTime < pmRefreshDateTime)
                    return new(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, 
                        pmRefreshDateTime.Hour, pmRefreshDateTime.Minute, pmRefreshDateTime.Second);
                else
                    return DateTime.MaxValue;
            case (int)ECafeInviteTimeSettingOptions.Immediately:
                return currentDateTime;
            default:
                return DateTime.MaxValue;
        }
    }

    //把已经到达时间的任务入列
    private void CreateCurrentTaskQueue()
    {
        //先判断有无已经到达时间的任务
        //int i = 0;
        //foreach (var item in _waitingTaskChainList)
        //    if (DateTime.Now >= item.ExecuteDateTime) i++;
        //if (i == 0) return;

        Queue<TaskModel> tempQueue0 = new();
        tempQueue0.Enqueue(new("启动游戏", "StartGame", GetOverrideJsonWithReadConfig(ETaskType.StartGame), ETaskType.StartGame));
        _currentTaskChainList.Add(new("启动游戏", DateTime.Now, false, true, tempQueue0));

        foreach (var taskChainItem in _waitingTaskChainList)
        {
            if (DateTime.Now >= taskChainItem.ExecuteDateTime)
            {
                //读取设置
                foreach(var taskItem in taskChainItem.TaskQueue)
                {
                    taskItem.PipelineOverride = GetOverrideJsonWithReadConfig(taskItem.Type);
                }
                _currentTaskChainList.Add(taskChainItem);
            }
        }

        Queue<TaskModel> tempQueue1 = new();
        tempQueue1.Enqueue(new("返回主界面", "ClickHomeButton", string.Empty, ETaskType.Normal));
        _currentTaskChainList.Add(new("返回主界面", DateTime.Now, false, false, tempQueue1));
        foreach (var item in _currentTaskChainList)
        {
            Debug.WriteLine(item.ExecuteDateTime + " - " + item.Name);
        }
    }

    //读取设置来生成所需的pipeline override json
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
                    string.Empty : "{\"RefreshGame@RecognizeDuplicatedLogin\":{\"next\":\"Stop\"}}";
            //1号咖啡厅邀请
            case ETaskType.Cafe1Invite:
                switch (_settingsData.Cafe1InviteSortTypeSettingIndex)
                {
                    case (int)ECafeInviteSortTypeSettingOptions.BondLvFromHighToLow:
                        overrideSortType = "好感等级";
                        overrideSortOrder = "SortHighToLow.png";
                        break;
                    case (int)ECafeInviteSortTypeSettingOptions.BondLvFromLowToHigh:
                        overrideSortType = "好感等级";
                        overrideSortOrder = "SortLowToHigh.png";
                        break;
                    case (int)ECafeInviteSortTypeSettingOptions.Pinned:
                        overrideSortType = "精选";
                        overrideSortOrder = "SortHighToLow.png";
                        break;
                    default:
                        overrideSortType = "好感等级";
                        overrideSortOrder = "SortHighToLow.png";
                        break;
                }
                overrideJson = "{" +
                    "\"CafeInvite@ChangeSortType\":{\"text\":\"" + overrideSortType + "\"}," +
                    "\"CafeInvite@RecognizeSortOrder\":{\"template\":\"" + overrideSortOrder + "\"}";
                if (_settingsData.Cafe1InviteIndexSettingIndex != 0)
                    overrideJson += ",\"CafeInvite@Invite\":{\"index\":" + _settingsData.Cafe1InviteIndexSettingIndex + "}";
                if (_settingsData.IsCafe1AllowInviteNeighboring is false)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupNeighboring\":{\"next\":\"CafeInvite@InviteCancel\"}";
                if (_settingsData.IsCafe1AllowInviteNeighboringSwapAlt is false)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupNeighboringSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"}";
                if (_settingsData.IsCafe1AllowInviteSwapAlt is false)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"}";
                overrideJson += "}";
                return overrideJson;
            //1号咖啡厅应用家具预设
            case ETaskType.Cafe1AMApplyLayout:
                return "{\"CafeLayout@ApplyLayout\":{\"index\":" + _settingsData.Cafe1AMApplyLayoutIndex + "}}";
            case ETaskType.Cafe1PMApplyLayout:
                return "{\"CafeLayout@ApplyLayout\":{\"index\":" + _settingsData.Cafe1PMApplyLayoutIndex + "}}";
            //2号咖啡厅邀请
            case ETaskType.Cafe2Invite:
                switch (_settingsData.Cafe2InviteSortTypeSettingIndex)
                {
                    case (int)ECafeInviteSortTypeSettingOptions.BondLvFromHighToLow:
                        overrideSortType = "好感等级";
                        overrideSortOrder = "SortHighToLow.png";
                        break;
                    case (int)ECafeInviteSortTypeSettingOptions.BondLvFromLowToHigh:
                        overrideSortType = "好感等级";
                        overrideSortOrder = "SortLowToHigh.png";
                        break;
                    case (int)ECafeInviteSortTypeSettingOptions.Pinned:
                        overrideSortType = "精选";
                        overrideSortOrder = "SortHighToLow.png";
                        break;
                    default:
                        overrideSortType = "好感等级";
                        overrideSortOrder = "SortHighToLow.png";
                        break;
                }
                overrideJson =
                    "{" +
                    "\"CafeInvite@ChangeSortType\":{\"text\":\"" + overrideSortType + "\"}," +
                    "\"CafeInvite@RecognizeSortOrder\":{\"template\":\"" + overrideSortOrder + "\"}";
                if (_settingsData.Cafe2InviteIndexSettingIndex != 0)
                    overrideJson += ",\"CafeInvite@Invite\":{\"index\":" + _settingsData.Cafe2InviteIndexSettingIndex + "}";
                if (_settingsData.IsCafe2AllowInviteNeighboring is false)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupNeighboring\":{\"next\":\"CafeInvite@InviteCancel\"}";
                if (_settingsData.IsCafe2AllowInviteNeighboringSwapAlt is false)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupNeighboringSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"}";
                if (_settingsData.IsCafe2AllowInviteSwapAlt is false)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"}";
                overrideJson += "}";
                return overrideJson;
            //2号咖啡厅应用家具预设
            case ETaskType.Cafe2AMApplyLayout:
                return "{\"CafeLayout@ApplyLayout\":{\"index\":" + _settingsData.Cafe2AMApplyLayoutIndex + "}}";
            case ETaskType.Cafe2PMApplyLayout:
                return "{\"CafeLayout@ApplyLayout\":{\"index\":" + _settingsData.Cafe2PMApplyLayoutIndex + "}}";
            default:
                return "";
        }
    }

    public void RemoveFromCurrentTaskQueue(TaskChainModel item)
    {
        _currentTaskChainList.Remove(item);
    }

    //从json里读取配置并使用配置初始化
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

    private void Test()
    {
        Debug.WriteLine(DateTime.Now + "  准备AutoConnect()");
        AutoConnect();
        Debug.WriteLine(DateTime.Now + "  准备X");
        //TaskCafeInvite.CheckTicketAvailable(_maaTasker);
        Debug.WriteLine(DateTime.Now + "  完成X");
        
    }
}