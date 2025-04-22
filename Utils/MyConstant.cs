namespace MaaBATapAssistant.Utils;

public static class MyConstant
{
    public const string AppVersion = "0.3.1";
    //public const string ResourceVersion = "0.0.0";
    public const string PlatformTag = "win-x";
    public const string GitHubProjectUrl = "https://github.com/ManuelLau/MaaBATapAssistant";
    public const string GitHubApiUrl = "https://api.github.com/repos/ManuelLau/MaaBATapAssistant/releases";
    public const string GiteeApiUrl = "https://gitee.com/api/v5/repos/manuel33/MaaBATapAssistant/releases";
    public const string BilibiliLink = "https://space.bilibili.com/3493267989596771";
    public const string BilibiliReadmeLink = "https://www.bilibili.com/opus/1058736840406728723";

    public static readonly string ConfigJsonDirectory = GetPath(@"config");
    public static readonly string ConfigJsonFilePath = GetPath(@"config\config.json");
    public static readonly string CacheFilePath = GetPath(@"config\cache.json"); //缓存文件也一起放config目录下
    public static readonly string LogFilePath = GetPath(@"debug\log.txt");
    public static readonly string ScreenshotImageDirectory = GetPath(@"images");

    public static readonly string MaaSourceDirectory = GetPath(@"resources\base");
    public static readonly string MaaSourcePathBiliBiliOverride = GetPath(@"resources\bilibili");
    public static readonly string MaaSourcePathZhtwOverride = GetPath(@"resources\zh-tw");
    public static readonly string MaaSourcePathEnOverride = GetPath(@"resources\en");
    public static readonly string MaaSourcePathJpOverride = GetPath(@"resources\jp");

    public static readonly TimeOnly RefreshTimeOnlyCN = new(4, 0, 0);
    public static readonly TimeOnly RefreshTimeOnlyNexon = new(3, 0, 0);

    public const int AutoRunEmulatorWaittingDefaultTimeSpan = 20; // 默认等待模拟器启动时间(秒)
    public const int AutoRunEmulatorWaittingMinTimeSpan = 10; // 最小等待模拟器启动时间(秒)
    public const int AutoRunEmulatorWaittingMaxTimeSpan = 600; // 最大等待模拟器启动时间(秒)
    public const int AutoSearchEmulatorWaittingTimeSpan = 100; // 最大等待搜索设备时间(秒)

    private static string GetPath(string path)
    {
        string appPath = AppDomain.CurrentDomain.BaseDirectory;
        return System.IO.Path.Combine(appPath, path);
    }
}
