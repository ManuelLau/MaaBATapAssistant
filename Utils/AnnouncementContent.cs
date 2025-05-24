namespace MaaBATapAssistant.Utils;

public static class AnnouncementContent
{
    public const string MainTitle = "公告";
    public const string LatestUpdateTitle = $"v{MyConstant.AppVersion}更新内容";
    public const string LatestUpdateContent =
        "*针对MuMu、雷电模拟器在自动退出模拟器时的优化，确保退出时关闭虚拟机以节省系统资源\n" +
        "*修复国际服日服无法直接执行2号咖啡厅邀请/应用家具预设任务的问题\n" + 
        "*更新MaaFramework到4.2版本";
    public const string NotesTitle = "使用须知";
    public const string NotesContent = "使用前请务必先查看使用说明文档！(README.md文件)";
}
