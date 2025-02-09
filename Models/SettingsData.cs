namespace MaaBATapAssistant.Models;

public class SettingsData
{
    // 所有设置项目
    public int ClientTypeSettingIndex { get; set; }
    public int Cafe1InviteTimeSettingIndex { get; set; }
    public int Cafe1InviteSortTypeSettingIndex { get; set; }
    public int Cafe1InviteIndexSettingIndex { get; set; }
    public bool IsCafe1AllowInviteNeighboring { get; set; }
    public bool IsCafe1AllowInviteNeighboringSwapAlt { get; set; }
    public bool IsCafe1AllowInviteSwapAlt { get; set; }
    public bool IsCafe1EnableApplyLayout {  get; set; }
    public int Cafe1AMApplyLayoutIndex {  get; set; }
    public int Cafe1PMApplyLayoutIndex {  get; set; }
    public int Cafe2InviteTimeSettingIndex { get; set; }
    public int Cafe2InviteSortTypeSettingIndex { get; set; }
    public int Cafe2InviteIndexSettingIndex { get; set; }
    public bool IsCafe2AllowInviteNeighboring { get; set; }
    public bool IsCafe2AllowInviteNeighboringSwapAlt { get; set; }
    public bool IsCafe2AllowInviteSwapAlt { get; set; }
    public bool IsCafe2EnableApplyLayout { get; set; }
    public int Cafe2AMApplyLayoutIndex { get; set; }
    public int Cafe2PMApplyLayoutIndex { get; set; }
    public bool IsReconnectAfterDuplicatedLogin { get; set; }
    public bool IsCloseGameAfterLastTask { get; set; }
    public bool IsCloseEmulatorAfterLastTask { get; set; }
    public bool IsAutoCheckResourceUpdate { get; set; }
    public bool IsAutoCheckAppUpdate { get; set; }
    public bool IsAutoUpdateResource { get; set; }
    public bool IsAutoUpdateApp { get; set; }
    public bool DoNotShowAnnouncementAgain { get; set; }
}
