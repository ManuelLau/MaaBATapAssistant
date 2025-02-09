namespace MaaBATapAssistant.Utils;

public static class MyConstant
{
    public const string AppVersion = "0.1.1";
    public const string ResourceVersion = "0.1.1";
    public const string PlatformTag = "win-x86";
    public const string ProjectUrl = "https://github.com/ManuelLau/MaaBATapAssistant";
    public const string GitHubApiUrl = "https://api.github.com/repos/ManuelLau/MaaBATapAssistant/releases";

    public const string ConfigJsonDirectory = "./config";
    public const string ConfigJsonFilePath = "./config/config.json";
    public const string CacheFilePath = "./config/cache.json"; //缓存文件也一起放config目录下
    public const string MaaSourcePath = "./resources/base";
    public const string MaaSourcePathBiliBiliOverride = "./resources/bilibili";
    public const string MaaSourcePathZhtwOverride = "./resources/zh-tw";
    public const string MaaSourcePathEnOverride = "./resources/en";
    public const string MaaSourcePathJpOverride = "./resources/jp";
    public static readonly TimeOnly RefreshTimeOnlyCN = new(4, 0, 0);
    public static readonly TimeOnly RefreshTimeOnlyNexon = new(3, 0, 0);
    /// <summary>
    /// 在刷新前3分钟不生成任何任务，用户自己手动操作
    /// </summary>
    public const int CreateTaskBeforeRefreshTimeSpan = 3;
}
