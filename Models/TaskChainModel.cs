using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaBATapAssistant.ViewModels;

namespace MaaBATapAssistant.Models;

public enum ETaskChainStatus
{
    Waiting, //等待进入执行任务队列
    InCurrentQueue, //时间已经到了，在执行任务的队列中，等待执行
    Running, //当前正在执行
}

/// <summary>
/// 任务列表item，表示一个任务链，可以包含多个小的Pipeline任务
/// </summary>
public partial class TaskChainModel : ObservableObject
{
    [ObservableProperty]
    public string name = string.Empty;
    [ObservableProperty]
    public DateTime executeDateTime;
    [ObservableProperty]
    public ETaskChainStatus status;

    public string StartLogMessage { get => "开始任务：" + Name; }
    public string SucceededLogMessage { get => Name + "已完成"; }
    public string FailedLogMessage { get => Name + "失败！"; }

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
        bool _needShowInWaitingTaskList,
        bool _needPrintLog,
        Queue<TaskModel> _taskQueue)
    {
        Name = _name;
        ExecuteDateTime = _executeDateTime;
        Status = ETaskChainStatus.Waiting;

        NeedShowInWaitingTaskList = _needShowInWaitingTaskList;
        NeedPrintLog = _needPrintLog;
        //StartLogMessage = startLogMessage;
        //SucceededLogMessage = succeededLogMessage;
        //FailedLogMessage = failedLogMessage;
        TaskQueue = _taskQueue;
        //Action = action;
    }

    [RelayCommand]
    public void DeleteTaskChain()
    {
        MainViewModel.Instance.DeleteTaskChain(this);
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
