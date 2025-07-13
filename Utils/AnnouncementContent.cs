namespace MaaBATapAssistant.Utils;

public static class AnnouncementContent
{
    public const string MainTitle = "公告";
    public const string LatestUpdateTitle = $"v{MyConstant.AppVersion}更新内容";
    public const string LatestUpdateContent =
        "*修复因初次加载慢导致无法进入困难关卡的问题\n" +
        "*修复因更换orc模型后导致无法扫荡困难关卡的问题\n" +
        "*修复国服在使用4.5th的pv时，卡在标题界面的问题\n" +
        "*区分资源文件与软件文件，简化资源文件更新流程\n" +
        "*增加对最新版mumu12 v5模拟器的支持，更新mumu5模拟器后需要重新设置模拟器路径\n 新路径：{安装目录}\\nx_device\\12.0\\shell\\MuMuNxDevice.exe";
    public const string NotesTitle = "使用须知";
    public const string NotesContent = "使用前请务必先查看使用说明文档！(README.md文件)";
    public const string UpdateHistoryMainTitle = "更新历史";
    public const string UpdateHistoryTitle0 = "v0.3.5更新内容";
    public const string UpdateHistoryContent0 =
        "*优化国际服登录检测流程，修复因\"特别任务活动\"按钮导致检测错误的问题\n" +
        "*统一使用同一个OCR模型";
}
