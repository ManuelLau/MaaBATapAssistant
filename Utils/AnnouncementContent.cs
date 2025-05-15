namespace MaaBATapAssistant.Utils;

public static class AnnouncementContent
{
    public const string MainTitle = "公告";
    public const string LatestUpdateTitle = $"v{MyConstant.AppVersion}更新内容";
    public const string LatestUpdateContent =
        "*适配国际服新版UI咖啡厅\n" +
        "*新功能：每日自动扫荡3次困难本（该任务只会在上午时间段生成，适用于买了体力卡的sensei，自动消耗掉60体力防止爆体）\n" + 
        "*优化：填写了模拟器路径后，点开始任务按钮的时候不会自动打开模拟器，只会在当前有任务可执行时打开模拟器\n" + 
        "*修复了某些情况下修改任务时间导致后续任务时间错误的BUG";
    public const string NotesTitle = "使用须知";
    public const string NotesContent = "使用前请务必先查看使用说明文档！(README.md文件)";
}
