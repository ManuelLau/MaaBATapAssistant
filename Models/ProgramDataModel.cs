using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace MaaBATapAssistant.Models;

#region 枚举声明
public enum EClientTypeSettingOptions
{
    [Description("官服")] Zh_CN = 0,
    [Description("B服")] Zh_CN_Bilibili,
    [Description("国际服-繁体中文")] Zh_TW,
    [Description("国际服-英文")] En,
    [Description("日服")] Jp,
};
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
#endregion

public partial class ProgramDataModel : ObservableObject
{
    /// <summary>挂机任务是否开始执行</summary>
    [ObservableProperty]
    public bool isAfkTaskRunning;
    /// <summary>当前任务是否正在执行</summary>
    [ObservableProperty]
    public bool isCurrentTaskExecuting;
    public SettingsData SettingsData { get; set; }

    private static readonly ProgramDataModel _instance = new();
    public static ProgramDataModel Instance
    {
        get => _instance ?? new();
    }

    public ProgramDataModel()
    {
        IsAfkTaskRunning = false;
        IsCurrentTaskExecuting = false;

        #region 设置配置文件默认值
        SettingsData = new()
        {
            ClientTypeSettingIndex = (int)EClientTypeSettingOptions.Zh_CN,
            Cafe1InviteTimeSettingIndex = (int)ECafeInviteTimeSettingOptions.AM,
            Cafe1InviteSortTypeSettingIndex = (int)ECafeInviteSortTypeSettingOptions.BondLvFromHighToLow,
            Cafe1InviteIndexSettingIndex = 0,
            IsCafe1AllowInviteNeighboring = true,
            IsCafe1AllowInviteNeighboringSwapAlt = true,
            IsCafe1AllowInviteSwapAlt = true,
            IsCafe1EnableApplyLayout = false,
            Cafe1AMApplyLayoutIndex = 0,
            Cafe1PMApplyLayoutIndex = 1,
            Cafe2InviteTimeSettingIndex = (int)ECafeInviteTimeSettingOptions.PM,
            Cafe2InviteSortTypeSettingIndex = (int)ECafeInviteSortTypeSettingOptions.BondLvFromHighToLow,
            Cafe2InviteIndexSettingIndex = 0,
            IsCafe2AllowInviteNeighboring = true,
            IsCafe2AllowInviteNeighboringSwapAlt = true,
            IsCafe2AllowInviteSwapAlt = true,
            IsCafe2EnableApplyLayout = false,
            Cafe2AMApplyLayoutIndex = 1,
            Cafe2PMApplyLayoutIndex = 0,
            IsReconnectAfterDuplicatedLogin = true, //后续改为默认false
            IsCloseGameAfterLastTask = false,
            IsCloseEmulatorAfterLastTask = false,
            IsAutoUpdateResource = false,
            IsAutoUpdateApp = false,
            DoNotShowAnnouncementAgain = true
        };
        #endregion
    }
    static ProgramDataModel()
    {

    }
}
