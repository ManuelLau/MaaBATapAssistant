using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace MaaBATapAssistant.Models;

public enum ETaskChainStatus
{
    Waiting, //等待进入执行任务队列
    InCurrentQueue, //时间已经到了，在执行任务的队列中，等待执行(未使用)
    Running, //当前正在执行
}
public enum ETaskChainType
{
    Tap,
    Invite,
    ApplyLayout,
    System, //系统自动添加的任务，比如启动游戏、重启游戏
}

/// <summary>
/// 任务列表item，表示一个任务链，可以包含多个小的Pipeline任务
/// </summary>
public partial class TaskChainModel : ObservableObject
{
    [ObservableProperty][JsonIgnore]
    public string name;
    [ObservableProperty][JsonIgnore]
    public DateTime executeDateTime;
    [ObservableProperty][JsonIgnore]
    public ETaskChainStatus status;

    public string OverrideStartLogMessage { get; set; }
    [JsonIgnore]
    public string StartLogMessage
    { 
        get => string.IsNullOrEmpty(OverrideStartLogMessage) ? "开始任务：" + Name : OverrideStartLogMessage;
    }
    [JsonIgnore]
    public string SucceededLogMessage { get => Name + "已完成"; }
    [JsonIgnore]
    public string FailedLogMessage { get => Name + "失败！"; }

    public ETaskChainType TaskChainType { get; set; }
    //是否在任务列表里显示出来
    public bool NeedShowInWaitingTaskList { get; set; }
    //是否需要在执行、成功、失败时候打Log
    public bool NeedPrintLog { get; set; }

    public Queue<TaskModel> TaskQueue { get; set; }
    //public Action Action { get; set; }

    /// <summary>不需要的string用string.Empty</summary>
    public TaskChainModel(
        string _name,
        DateTime _executeDateTime,
        ETaskChainType _taskChainType,
        bool _needShowInWaitingTaskList,
        bool _needPrintLog,
        string _overrideStartLogMessage,
        Queue<TaskModel> _taskQueue)
    {
        Name = _name;
        ExecuteDateTime = _executeDateTime;
        Status = ETaskChainStatus.Waiting;

        TaskChainType = _taskChainType;
        NeedShowInWaitingTaskList = _needShowInWaitingTaskList;
        NeedPrintLog = _needPrintLog;
        TaskQueue = _taskQueue;
        OverrideStartLogMessage = _overrideStartLogMessage;
        //Action = action;
    }

    //public bool Run()
    //{
    //    try
    //    {
    //        Action();
    //        return true;
    //    }
    //    catch (Exception)
    //    {
    //        return false;
    //    }
    //}
}
