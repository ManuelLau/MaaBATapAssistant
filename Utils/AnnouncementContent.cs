namespace MaaBATapAssistant.Utils;

public static class AnnouncementContent
{
    public const string MainTitle = "公告";
    public const string LatestUpdateTitle = $"v{MyConstant.AppVersion}更新内容";
    public const string LatestUpdateContent =
        "新增沙勒2.5fes特别签到页面的识别";
    public const string NotesTitle = "使用须知";
    public const string NotesContent = "使用前请务必先查看使用说明文档！(README.md文件)";
    public const string UpdateHistory= "更新历史";
    public const string UpdateHistoryTitle0 = "v0.3.1更新内容";
    public const string UpdateHistoryContent0 =
        "新功能：执行邀请前自动截图弹窗内容，以便查看邀请历史记录，截图存放在images文件夹中，最多保存10个\n" +
        "新功能：增加避开时间段设置项，小助手不会自动生成该时间段内的任务\n" +
        "新功能：完成当前任务后自动退出游戏或模拟器\n" +
        "增加任务列表中下下个时间段任务的预览，方便提前规划\n" +
        "修复日服进入咖啡厅时体力满了导致检测出错的bug";
}
