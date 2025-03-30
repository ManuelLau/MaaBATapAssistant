using CommunityToolkit.Mvvm.ComponentModel;
using MaaBATapAssistant.Utils;
using Newtonsoft.Json;

namespace MaaBATapAssistant.Models;

public partial class SettingsDataModel : ObservableObject
{
    // 所有设置项目
    public int ClientTypeSettingIndex { get; set; } = (int)EClientTypeSettingOptions.Zh_CN;
    [ObservableProperty][JsonIgnore]
    public int cafe1InviteTimeSettingIndex = (int)ECafeInviteTimeSettingOptions.AM;
    public int Cafe1InviteSortTypeSettingIndex { get; set; } = (int)ECafeInviteSortTypeSettingOptions.BondLvFromHighToLow;
    public int Cafe1InviteIndexSettingIndex { get; set; } = 0;
    public bool IsCafe1AllowInviteNeighboring { get; set; } = true;
    public bool IsCafe1AllowInviteNeighboringSwapAlt { get; set; } = true;
    public bool IsCafe1AllowInviteSwapAlt { get; set; } = true;
    [ObservableProperty][JsonIgnore]
    public bool isCafe1EnableApplyLayout = false;
    public int Cafe1AMApplyLayoutIndex { get; set; } = 0;
    public int Cafe1PMApplyLayoutIndex { get; set; } = 1;
    [ObservableProperty][JsonIgnore]
    public int cafe2InviteTimeSettingIndex = (int)ECafeInviteTimeSettingOptions.PM;
    public int Cafe2InviteSortTypeSettingIndex { get; set; } = (int)ECafeInviteSortTypeSettingOptions.BondLvFromHighToLow;
    public int Cafe2InviteIndexSettingIndex { get; set; } = 0;
    public bool IsCafe2AllowInviteNeighboring { get; set; } = true;
    public bool IsCafe2AllowInviteNeighboringSwapAlt { get; set; } = true;
    public bool IsCafe2AllowInviteSwapAlt { get; set; } = true;
    [ObservableProperty][JsonIgnore]
    public bool isCafe2EnableApplyLayout = false;
    public int Cafe2AMApplyLayoutIndex { get; set; } = 1;
    public int Cafe2PMApplyLayoutIndex { get; set; } = 0;
    public bool IsRelationshipRankUpAutoScreenShot { get; set; } = true;
    public bool IsReconnectAfterDuplicatedLogin { get; set; } = true;
    //public bool IsAutoCheckResourceUpdate { get; set; } = false; // 未使用
    public bool IsAutoCheckAppUpdate { get; set; } = true;
    //public bool IsAutoUpdateResource { get; set; } = false; // 未使用
    //public bool IsAutoUpdateApp { get; set; } = false; // 未使用
    [ObservableProperty]
    public string emulatorPath = string.Empty;
    [ObservableProperty]
    public int autoRunEmulatorWaittingTimeSpan = MyConstant.AutoRunEmulatorWaittingDefaultTimeSpan;
    public bool DoNotShowAnnouncementAgain { get; set; } = false;
}
