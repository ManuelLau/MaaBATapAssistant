using MaaBATapAssistant.ViewModels;
using MaaBATapAssistant.Models;
using MaaFramework.Binding;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading;

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
    private readonly List<TaskChainModel> _currentTaskChainList = [];
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
            Utility.PrintLog("暂无任务，请先生成任务");
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
        Utility.PrintLog("正在停止...");
        MainViewModel.Instance.IsStoppingCurrentTask = true;
        foreach (var taskChainTtem in _waitingTaskChainList)
        {
            taskChainTtem.Status = ETaskChainStatus.Waiting;
        }
        PreventSleepTool.RestoreSleep();
        _cancellationTokenSource.Cancel();
        await Task.Run(MaaTaskerDispose);
    }

    //生成从给定时间到下一次咖啡厅刷新时间前的任务
    public DateTime CreateTask(DateTime taskDateTime)
    {
        DateTime nextServerRefreshDateTime = GetNextServerRefreshDateTime(taskDateTime); //04:00 or 03:00
        DateTime nextCafeRefreshDateTime = GetNextCafeRefreshDateTime(taskDateTime);
        bool hasCreatedCafe1InviteTask = false;
        bool hasCreatedCafe2InviteTask = false;
        bool hasCreatedCafe1AMApplyLayoutTask = false;
        bool hasCreatedCafe1PMApplyLayoutTask = false;
        bool hasCreatedCafe2AMApplyLayoutTask = false;
        bool hasCreatedCafe2PMApplyLayoutTask = false;
        //循环生成第一轮任务(直到下一次咖啡厅刷新时间)
        while (taskDateTime < nextCafeRefreshDateTime)
        {
            CreateTaskFragment(taskDateTime, ref hasCreatedCafe1InviteTask, ref hasCreatedCafe2InviteTask, ref hasCreatedCafe1AMApplyLayoutTask,
                ref hasCreatedCafe1PMApplyLayoutTask, ref hasCreatedCafe2AMApplyLayoutTask, ref hasCreatedCafe2PMApplyLayoutTask);
            taskDateTime = taskDateTime.AddHours(3);
        }
        taskDateTime = nextCafeRefreshDateTime;
        //如果时间到达服务器刷新时间，就添加一个重启任务
        if (taskDateTime == nextServerRefreshDateTime)
        {
            Queue<TaskModel> taskQueue = new();
            taskQueue.Enqueue(new("重启游戏", "RestartGame", string.Empty, ETaskType.RestartGame));
            AddToWaitingTaskList(new("重启游戏", taskDateTime, true, true, "日期已变更，重启游戏中...", taskQueue));
        }
        return taskDateTime;
        //对任务按时间排序?
    }

    //自动连接模拟器和Maa资源，选择搜索到的第一个模拟器
    private bool AutoConnect()
    {
        var devices = _maaToolkit.AdbDevice.Find();
        if (devices.IsEmpty)
        {
            Utility.PrintLog("找不到模拟器，请先手动打开模拟器");
            return false;
        }

        if (!LoadMaaSource(out MaaResource? maaResource) || maaResource == null)
            return false;

        if (_maaTasker != null)
            if (_maaTasker.Initialized)
            {
                Utility.PrintLog("已连接至模拟器" + devices[0].Name);
                return true;
            }

        Utility.PrintLog("正在连接模拟器...");
        try
        {
            _maaTasker = new()
            {
                //默认使用第0个设备，后续再改
                Controller = devices[0].ToAdbController(),
                Resource = maaResource,
                DisposeOptions = DisposeOptions.All,
            };
        }
        catch (Exception)
        {
            Utility.PrintLog("设备初始化失败，可能是ADB连接出现问题，请尝试重启模拟器、ADB或者重启系统");
            return false;
        }
        if (_maaTasker == null || !_maaTasker.Initialized)
        {
            Utility.PrintLog("设备初始化失败，可能是ADB连接出现问题，请尝试重启模拟器、ADB或者重启系统");
            return false;
        }
        Utility.PrintLog("成功连接至模拟器" + devices[0].Name);
        return true;
    }

    private static bool LoadMaaSource(out MaaResource? resource)
    {
        resource = null;
        //根据配置里的客户端类型选项改变读取的文件路径
        string[] maaSourcePaths = ProgramDataModel.Instance.SettingsData.ClientTypeSettingIndex switch
        {
            (int)EClientTypeSettingOptions.Zh_CN => [MyConstant.MaaSourcePath],
            (int)EClientTypeSettingOptions.Zh_CN_Bilibili => [MyConstant.MaaSourcePath, MyConstant.MaaSourcePathBiliBiliOverride],
            _ => [MyConstant.MaaSourcePath],
        };
        try
        {
            resource = new(maaSourcePaths);
        }
        catch (Exception)
        {
            Utility.PrintLog("加载资源文件失败");
            return false;
        }
        return true;
    }

    //开启挂机任务
    private async Task StartAfkTask(CancellationToken token)
    {
        bool firstTimeExecute = true;
        DateTime nextCafeRefreshDateTime = GetNextCafeRefreshDateTime(DateTime.Now);
        DateTime nextServerRefreshDateTime = GetNextServerRefreshDateTime(DateTime.Now);

        while (!token.IsCancellationRequested)
        {
            // 先清除上一个时间段的旧任务
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

            if (DateTime.Now >= nextCafeRefreshDateTime)
            {
                // 到达咖啡厅刷新时间，自动生成12小时后的任务队列
                CreateTask(GetNextCafeRefreshDateTime(nextCafeRefreshDateTime));
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
                    ExecuteCurrentTask(_cancellationTokenSource.Token);
                    Utility.MyDebugWriteLine($"已入列的任务执行完成或者已停止");
                    ProgramDataModel.Instance.IsCurrentTaskExecuting = false;
                    break;
                }
            }
            if(firstTimeExecute)
            {
                Utility.PrintLog("暂无可执行任务，小助手待机中");
                firstTimeExecute = false;
            }
            await Task.Delay(1000, CancellationToken.None); // 每秒检查一次
        }
        StopCurrentTask();
    }

    private void ExecuteCurrentTask(CancellationToken token)
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

            Utility.MyDebugWriteLine($"准备执行任务链 - {currentTaskChain.Name}");
            bool anyTaskFailed = false;
            if (currentTaskChain.NeedPrintLog)
            {
                Utility.PrintLog(currentTaskChain.StartLogMessage);
            }
            if (_maaTasker == null)
            {
                Utility.MyDebugWriteLine($"maaTasker is null!");
                Utility.PrintLog("设备失去连接！");
                return;
            }
            var device = _maaTasker.Toolkit.AdbDevice.Find();
            if (device.IsEmpty || device.IsInvalid)
            {
                Utility.MyDebugWriteLine($"设备失去连接！");
                Utility.PrintLog("设备失去连接！");
                Stop();
                return;
            }
            foreach (var taskItem in currentTaskChain.TaskQueue)
            {
                Utility.MyDebugWriteLine($"准备执行单个任务 - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry})");
                var status = taskItem.PipelineOverride == "" ?
                    _maaTasker.AppendTask(taskItem.Entry).Wait() :
                    _maaTasker.AppendTask(taskItem.Entry, taskItem.PipelineOverride).Wait();
                if (status.IsSucceeded())
                {
                    Utility.MyDebugWriteLine($"执行单个任务完成 - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry})");
                }
                else
                {
                    anyTaskFailed = true;
                    Utility.MyDebugWriteLine($"执行单个任务失败! - {taskItem.Name} - maaTasker.AppendPipeline({taskItem.Entry}) - {status}");

                    // 检测是否因为用户停止任务导致释放了maaTasker，如果不break会导致闪退
                    if (token.IsCancellationRequested)
                    {
                        Utility.MyDebugWriteLine("停止任务,break - 跳出当前任务链的任务队列遍历");
                        break;
                    }
                    // 不是手动停止的话,有Pipeline失败后会继续执行后面的Pipeline
                }
            }
            
            if (anyTaskFailed)
            {
                if (currentTaskChain.NeedPrintLog)
                    Utility.PrintLog(currentTaskChain.FailedLogMessage);
                Utility.MyDebugWriteLine($"执行任务链过程中出现异常！ - {currentTaskChain.Name}");
            }
            else
            {
                if (currentTaskChain.NeedPrintLog)
                    Utility.PrintLog(currentTaskChain.SucceededLogMessage);
                Utility.MyDebugWriteLine($"执行任务链完成 - {currentTaskChain.Name}");
            }
            //移除任务链。如果是因为用户手动取消的话就不移除，并将状态设为等待中
            if (token.IsCancellationRequested)
            {
                currentTaskChain.Status = ETaskChainStatus.Waiting;
                return;
            }
            _currentTaskChainList.Remove(currentTaskChain);
            if (currentTaskChain.NeedShowInWaitingTaskList)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _waitingTaskChainList.Remove(currentTaskChain);
                });
            }
        }

        DateTime endDateTime = DateTime.Now;
        // 使用初始时间判断下一次刷新时间，以免执行期间刚好跨过咖啡厅刷新时间(To-do 待优化，以后检测到到达刷新时间自动终止当前所有任务)
        DateTime nextCafeRefreshDateTime = GetNextCafeRefreshDateTime(startDateTime);
        if (endDateTime.AddHours(3) < nextCafeRefreshDateTime)
        {
            // 刷新后续任务时间，如果+3小时后时间还没到执行时间，则不用刷新任务时间。默认以用户输入的时间为准
            if (_waitingTaskChainList[0].ExecuteDateTime < endDateTime.AddHours(3))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _waitingTaskChainList[0].ExecuteDateTime = endDateTime.AddHours(3);
                    for (int i = 1; i < _waitingTaskChainList.Count - 1; i++)
                    {
                        //只处理当前时间段的
                        if (_waitingTaskChainList[i].ExecuteDateTime < nextCafeRefreshDateTime)
                        {
                            if (_waitingTaskChainList[i].ExecuteDateTime < _waitingTaskChainList[i - 1].ExecuteDateTime.AddHours(3))
                            {
                                _waitingTaskChainList[i].ExecuteDateTime = _waitingTaskChainList[i - 1].ExecuteDateTime.AddHours(3);
                                //排除延迟后跨时间段的任务
                                if (_waitingTaskChainList[i].ExecuteDateTime >= nextCafeRefreshDateTime)
                                {
                                    _waitingTaskChainList.RemoveAt(i);
                                    i--;
                                }
                            }
                        }
                    }
                });
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
        Utility.PrintLog("已停止");
    }
    
    //获取下一个服务器刷新的日期时间。读取"客户端类型"设置项来返回不同的时间
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
    
    //获取下一个咖啡厅刷新的日期时间
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

    //生成一个任务片段，该片段中的任务链共用一个执行时间。邀请和应用家具预设的任务链在每个时间段只会生成一次
    private void CreateTaskFragment(DateTime executeDateTime, ref bool _hasCreatedCafe1InviteTask, ref bool _hasCreatedCafe2InviteTask,
        ref bool _hasCreatedCafe1AMApplyLayoutTask, ref bool _hasCreatedCafe1PMApplyLayoutTask, 
        ref bool _hasCreatedCafe2AMApplyLayoutTask, ref bool _hasCreatedCafe2PMApplyLayoutTask)
    {
        DateTime nextServerRefreshDateTime = GetNextServerRefreshDateTime(DateTime.Now); //04:00 or 03:00
        DateTime pmRefreshDateTime = nextServerRefreshDateTime.AddHours(-12); //16:00
        Queue<TaskModel> tempQueue = new();
        
        //生成摸头任务
        tempQueue.Enqueue(new("进入1号咖啡厅", "MoveToCafe1", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("(1号)咖啡厅摸头", "CafeTap", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
        tempQueue.Enqueue(new("(2号)咖啡厅摸头", "CafeTap", string.Empty, ETaskType.Normal));
        AddToWaitingTaskList(new("咖啡厅摸头", executeDateTime, true, true, string.Empty, tempQueue));
        
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
                AddToWaitingTaskList(new("2号咖啡厅邀请", inviteTime, true, true, string.Empty, tempQueue));
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
                AddToWaitingTaskList(new("2号咖啡厅应用家具预设", executeDateTime, true, true, string.Empty, tempQueue));
                _hasCreatedCafe2AMApplyLayoutTask = true;
            }
            if (_hasCreatedCafe2PMApplyLayoutTask == false && executeDateTime >= pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入2号咖啡厅", "MoveToCafe2", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("2号咖啡厅应用家具预设PM", "CafeApplyLayout", string.Empty, ETaskType.Cafe2PMApplyLayout));
                AddToWaitingTaskList(new("2号咖啡厅应用家具预设", executeDateTime, true, true, string.Empty, tempQueue));
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
                AddToWaitingTaskList(new("1号咖啡厅邀请", inviteTime, true, true, string.Empty, tempQueue));
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
                AddToWaitingTaskList(new("1号咖啡厅应用家具预设", executeDateTime, true, true, string.Empty, tempQueue));
                _hasCreatedCafe1AMApplyLayoutTask = true;
            }
            if (_hasCreatedCafe1PMApplyLayoutTask == false && executeDateTime >= pmRefreshDateTime)
            {
                tempQueue = new();
                tempQueue.Enqueue(new("进入1号咖啡厅", "MoveToCafe1", string.Empty, ETaskType.Normal));
                tempQueue.Enqueue(new("1号咖啡厅应用家具预设", "CafeApplyLayout", string.Empty, ETaskType.Cafe1PMApplyLayout));
                AddToWaitingTaskList(new("1号咖啡厅应用家具预设", executeDateTime, true, true, string.Empty, tempQueue));
                _hasCreatedCafe1PMApplyLayoutTask = true;
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
        if (_waitingTaskChainList[0].TaskQueue.Peek().Type != ETaskType.RestartGame)
        {
            Queue<TaskModel> tempQueue0 = new();
            tempQueue0.Enqueue(new("启动游戏", "StartGame", GetOverrideJsonWithReadConfig(ETaskType.StartGame), ETaskType.StartGame));
            _currentTaskChainList.Add(new("启动游戏", DateTime.Now, false, true, string.Empty, tempQueue0));
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
                _currentTaskChainList.Add(taskChainItem);
            }
        }

        Queue<TaskModel> tempQueue1 = new();
        tempQueue1.Enqueue(new("返回主界面", "ClickHomeButton", string.Empty, ETaskType.Normal));
        _currentTaskChainList.Add(new("返回主界面", DateTime.Now, false, false, string.Empty, tempQueue1));
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
                if (!_settingsData.IsCafe1AllowInviteNeighboring)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupNeighboring\":{\"next\":\"CafeInvite@InviteCancel\"}";
                if (!_settingsData.IsCafe1AllowInviteNeighboringSwapAlt)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupNeighboringSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"}";
                if (!_settingsData.IsCafe1AllowInviteSwapAlt)
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
                if (!_settingsData.IsCafe2AllowInviteNeighboring)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupNeighboring\":{\"next\":\"CafeInvite@InviteCancel\"}";
                if (!_settingsData.IsCafe2AllowInviteNeighboringSwapAlt)
                    overrideJson += ",\"CafeInvite@InviteConfirmPopupNeighboringSwapAlt\":{\"next\":\"CafeInvite@InviteCancel\"}";
                if (!_settingsData.IsCafe2AllowInviteSwapAlt)
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

    public void RemoveFromCurrentTaskChainQueue(TaskChainModel item)
    {
        _currentTaskChainList.Remove(item);
    }

    public void MaaTaskerDispose()
    {
        if (_maaTasker != null)
        {
            _maaTasker.Abort().Wait();
            _maaTasker.Dispose();
        }
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
}