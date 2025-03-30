namespace MaaBATapAssistant.Utils;

public static class MyConstant
{
    public const string AppVersion = "0.3.0";
    //public const string ResourceVersion = "0.0.0";
    public const string PlatformTag = "win-x";
    public const string GitHubProjectUrl = "https://github.com/ManuelLau/MaaBATapAssistant";
    public const string GitHubApiUrl = "https://api.github.com/repos/ManuelLau/MaaBATapAssistant/releases";
    public const string GiteeApiUrl = "https://gitee.com/api/v5/repos/manuel33/MaaBATapAssistant/releases";
    public const string BilibiliLink = "https://space.bilibili.com/3493267989596771";

    public const string ConfigJsonDirectory = @".\config";
    public const string ConfigJsonFilePath = @".\config\config.json";
    public const string CacheFilePath = @".\config\cache.json"; //缓存文件也一起放config目录下
    public const string MaaSourcePath = @".\resources\base";
    public const string LogFilePath = @".\debug\log.txt";
    public const string ScreenshotImagePath = @".\images";

    public const string MaaSourcePathBiliBiliOverride = @".\resources\bilibili";
    public const string MaaSourcePathZhtwOverride = @".\resources\zh-tw";
    public const string MaaSourcePathEnOverride = @".\resources\en";
    public const string MaaSourcePathJpOverride = @".\resources\jp";

    public static readonly TimeOnly RefreshTimeOnlyCN = new(4, 0, 0);
    public static readonly TimeOnly RefreshTimeOnlyNexon = new(3, 0, 0);

    public const int AutoRunEmulatorWaittingDefaultTimeSpan = 30; // 默认等待模拟器启动时间(秒)
    public const int AutoRunEmulatorWaittingMinTimeSpan = 10; // 最小等待模拟器启动时间(秒)
    public const int AutoRunEmulatorWaittingMaxTimeSpan = 600; // 最大等待模拟器启动时间(秒)
    public const int AutoSearchEmulatorWaittingTimeSpan = 100; // 最大等待搜索设备时间(秒)
}
