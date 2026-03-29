using System.ComponentModel;

namespace MaaBATapAssistant.Models;

// 枚举声明

public enum EClientTypeSettingOptions
{
    [Description("模拟器 官服")] Zh_CN = 0,
    [Description("模拟器 B服")] Zh_CN_Bilibili,
    [Description("模拟器 国际服(繁体中文)")] Zh_TW,
    [Description("模拟器 日服")] Jp,
    [Description("PC端 国际服(繁体中文)")] Zh_TW_PC,
    //[Description("PC端 日服")] Jp_PC,
}

public enum ECafeInviteTimeSettingOptions
{
    [Description("4:00~15:59")] AM = 0,
    [Description("16:00~次日3:59")] PM,
    [Description("立即，无视重置时间")] Immediately,
    [Description("不使用")] DoNotUse,
};
public enum ECafeInviteSortTypeSettingOptions
{
    [Description("好感由高到低")] BondLvFromHighToLow = 0,
    [Description("好感由低到高")] BondLvFromLowToHigh,
    [Description("精选排序")] Pinned,
};
public enum EDownloadSource
{
    Gitee,
    GitHub
}