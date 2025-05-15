using MaaBATapAssistant.Models;
using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using System.IO;
using System.Windows.Media.Imaging;

namespace MaaBATapAssistant.Utils;

/// <summary>
/// 自定义任务，注意：需要在TaskManager.cs中注册
/// </summary>
public static class CustomTask
{
    public static void ScreenShot(AnalyzeArgs args, string filePrefix)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = args.Image.EncodedDataStream;
        bitmap.EndInit();
        bitmap.Freeze(); // 冻结图像以提高性能
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        if (!Directory.Exists(MyConstant.ScreenshotImageDirectory))
        {
            Directory.CreateDirectory(MyConstant.ScreenshotImageDirectory);
        }
        string filePath = Path.Combine(MyConstant.ScreenshotImageDirectory, $"{filePrefix}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png");
        using FileStream fileStream = new(filePath, FileMode.Create);
        encoder.Save(fileStream);
    }

    public class RelationshipRankUpScreenshot : IMaaCustomRecognition
    {
        public string Name { get; set; } = nameof(RelationshipRankUpScreenshot);

        public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
        {
            if (ProgramDataModel.Instance.SettingsData.IsRelationshipRankUpAutoScreenShot)
            {
                ScreenShot(args, "RelationshipRankUp");
                Utility.PrintLog("好感等级提升，已自动截图");
            }
            return true;
        }
    }

    public class InviteUnavailableSkipTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(InviteUnavailableSkipTask);

        public bool Run(in IMaaContext context, in RunArgs args)
        {
            Utility.PrintError("咖啡厅邀请失败，邀请券冷却中");
            TaskManager.Instance.CurrentTaskChainPrintFinishedLog = false;
            return true;
        }
    }

    public class InviteScreenshot : IMaaCustomRecognition
    {
        public string Name { get; set; } = nameof(InviteScreenshot);

        public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
        {
            ScreenShot(args, "Invite");

            // 获取所有截图，并按创建日期排序。同一分钟创建的文件，会根据文件名再排序，时间早的在前面
            var files = Directory.GetFiles(MyConstant.ScreenshotImageDirectory, "Invite-*.png")
                                 .Select(file => new FileInfo(file)).OrderBy(file => file.CreationTime).ToList();
            // 最多保存10个截图
            const short maxScreenshotCount = 10;
            while (files.Count > maxScreenshotCount)
            {
                File.Delete(files.First().FullName);
                files.RemoveAt(0);
            }
            return true;
        }
    }

    public class InviteCancelNotify : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(InviteCancelNotify);

        public bool Run(in IMaaContext context, in RunArgs args)
        {
            Utility.PrintLog("规则不符，邀请已取消");
            TaskManager.Instance.CurrentTaskChainPrintFinishedLog = false;
            return true;
        }
    }

    public class DuplicatedLoginStopTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(DuplicatedLoginStopTask);

        public bool Run(in IMaaContext context, in RunArgs args)
        {
            Utility.PrintLog("发现重复登录，任务即将停止");
            TaskManager.Instance.Stop(true);
            return true;
        }
    }

    public class MaintenanceStopTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(MaintenanceStopTask);

        public bool Run(in IMaaContext context, in RunArgs args)
        {
            //Utility.PrintLog("服务器维护，任务将延后半小时执行");
            Utility.PrintLog("服务器维护，任务即将停止");
            TaskManager.Instance.Stop(true);
            return true;
        }
    }

    public class ClientUpdateStopTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(ClientUpdateStopTask);

        public bool Run(in IMaaContext context, in RunArgs args)
        {
            Utility.PrintError("游戏客户端需要更新，任务即将停止。请手动更新后再启动任务");
            TaskManager.Instance.Stop(true);
            return true;
        }
    }

    public class IPBlockedStopTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(IPBlockedStopTask);

        public bool Run(in IMaaContext context, in RunArgs args)
        {
            Utility.PrintError("登录游戏失败（ip受限）！请开启加速器后再尝试");
            TaskManager.Instance.Stop(true);
            return true;
        }
    }

    public class PrintSweepError : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(PrintSweepError);

        public bool Run(in IMaaContext context, in RunArgs args)
        {
            Utility.PrintError($"扫荡关卡H{ProgramDataModel.Instance.SettingsData.HardLevel}失败，体力不足或次数不足");
            TaskManager.Instance.CurrentTaskChainPrintFinishedLog = false;
            return true;
        }
    }

    public class PrintFindeLevelError : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(PrintFindeLevelError);

        public bool Run(in IMaaContext context, in RunArgs args)
        {
            Utility.PrintError($"寻找关卡H{ProgramDataModel.Instance.SettingsData.HardLevel}失败，请确认关卡填写正确或关卡已解锁");
            TaskManager.Instance.CurrentTaskChainPrintFinishedLog = false;
            return true;
        }
    }

    public class SweepDropScreenshot : IMaaCustomRecognition
    {
        public string Name { get; set; } = nameof(SweepDropScreenshot);

        public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
        {
            ScreenShot(args, "Drop");

            // 获取所有截图，并按创建日期排序。同一分钟创建的文件，会根据文件名再排序，时间早的在前面
            var files = Directory.GetFiles(MyConstant.ScreenshotImageDirectory, "Drop-*.png")
                                 .Select(file => new FileInfo(file)).OrderBy(file => file.CreationTime).ToList();
            // 最多保存10个截图
            const short maxScreenshotCount = 10;
            while (files.Count > maxScreenshotCount)
            {
                File.Delete(files.First().FullName);
                files.RemoveAt(0);
            }
            return true;
        }
    }
}
