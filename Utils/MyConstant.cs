namespace MaaBATapAssistant.Utils;

public static class MyConstant
{
    public const string AppVersion = "0.1.0";
    public const string ResourceVersion = "0.1.0";
    public const string ConfigJsonFilePath = "./config/config.json";
    public const string MaaSourcePath = "./resource/base";
    public const string MaaSourcePathBiliBiliOverride = "./resource/bilibili";
    public const string MaaSourcePathCHTOverride = "./resource/cht";
    public static readonly TimeOnly RefreshTimeOnlyCN = new(4, 0, 0);
    public static readonly TimeOnly RefreshTimeOnlyNexon = new(3, 0, 0);
    /// <summary>
    /// 在刷新前3分钟不生成任何任务，用户自己手动操作
    /// </summary>
    public const int CreateTaskBeforeRefreshTimeSpan = 3;
    public const string CloseBAPipelineEntry = "CloseGame";

    public const string ProjectUrl = "https://github.com/ManuelLau/MaaBATapAssistant";
}
