namespace MaaBATapAssistant.Utils;

public static class AnnouncementContent
{
    public const string MainTitle = "公告";
    public const string LatestUpdateTitle = $"v{MyConstant.AppVersion}更新内容";
    public const string LatestUpdateContent =
        "*优化国际服登录检测流程，修复因\"特别任务活动\"按钮导致检测错误的问题\n" +
        "*统一使用同一个OCR模型";
    public const string NotesTitle = "使用须知";
    public const string NotesContent = "使用前请务必先查看使用说明文档！(README.md文件)";
}
