namespace MaaBATapAssistant.Models;

//非Normal、RestartGame类型表示会根据设置来覆写Pipeline
public enum ETaskType
{
    Normal = 0,
    StartGame,
    RestartGame,
    Cafe1Invite,
    Cafe2Invite,
    Cafe1AMApplyLayout,
    Cafe1PMApplyLayout,
    Cafe2AMApplyLayout,
    Cafe2PMApplyLayout,
}

//单个Pipeline任务
public class TaskModel
{
    public string Name;
    /// <summary>可以根据Entry是否为empty来判断是哪种类型任务</summary>
    public string Entry { get; set; }
    public string PipelineOverride { get; set; }
    public ETaskType Type { get; set; }

    /// <summary>不需要的string用string.Empty</summary>
    public TaskModel(string name, string entry, string pipelineOverride, ETaskType type)
    {
        Name = name;
        Entry = entry;
        PipelineOverride = pipelineOverride;
        Type = type;
    }
}
