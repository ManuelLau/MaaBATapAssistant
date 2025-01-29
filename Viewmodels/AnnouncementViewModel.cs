using CommunityToolkit.Mvvm.ComponentModel;
using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;
namespace MaaBATapAssistant.ViewModels;

public partial class AnnouncementViewModel : ObservableObject
{
    private static readonly AnnouncementViewModel _instance = new();
    public static AnnouncementViewModel Instance
    {
        get => _instance ?? new();
    }

    [ObservableProperty]
    public ProgramDataModel programData = ProgramDataModel.Instance;

    [ObservableProperty]
    public string mainTitle = "公告";
    [ObservableProperty]
    public string title0 = "使用须知";
    [ObservableProperty]
    public string str0 = "使用挂机脚本可能会违反用户协议及相关条款，悠星有权采取包括但不限于封禁账号、限制账号权限等措施。" +
        "如果您使用该脚本的任何功能，即表明您已了解相关风险并对自己的行为承担全部责任。";
    [ObservableProperty]
    public string title1 = $"V{MyConstant.AppVersion} 更新内容";
    [ObservableProperty]
    public string str1 = "初始测试版本，基础功能已经可以正常使用了(也许可能大概会有一些奇奇怪怪的bug)" +
        "\n由于还没做新版本检查/自动更新的功能，还请时不时手动前往github查看有没有更新。" +
        "\n使用前请务必先查看文档！";
    [ObservableProperty]
    public string title2 = "近期计划更新内容";
    [ObservableProperty]
    public string str2 = "1、一些小功能/小优化" +
        "\n2、自定义任务避开时间：可以设置一个时间段，小助手不会生成在该时间段内的任务" +
        "\n3、程序、资源文件自动更新" +
        "\n4、国际服、日服的适配：待功能稳定后再添加" +
        "\n5、多语言";
}
