using MaaBATapAssistant.Models;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using System.IO;

namespace MaaBATapAssistant.Utils;

/// <summary>
/// 自定义任务，注意：需要在TaskManager.cs中注册
/// </summary>
public static class CustomTask
{
    private static bool ScreenShot(IMaaContext context, string filePrefix)
    {
        var controller = context.Tasker.Controller;
        controller.Screencap().Wait();
        MaaImageBuffer imageBuffer = new();
        bool success = controller.GetCachedImage(imageBuffer);

        if (success)
        {
            if (imageBuffer.TryGetEncodedData(out byte[]? imageData))
            {
                if (imageData is not null)
                {
                    string filePath = Path.Combine(Constants.ScreenshotImageDirectory, $"{filePrefix}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png");
                    File.WriteAllBytes(filePath, imageData);
                    return true;
                }
                else
                {
                    Utility.MyDebugWriteLine("截图数据为空！");
                    return false;
                }
            }
            else
            {
                Utility.MyDebugWriteLine("截图失败！- TryGetEncodedData()");
                return false;
            }
        }
        else
        {
            Utility.MyDebugWriteLine("截图失败！- GetCachedImage()");
            return false;
        }
    }

    public class RelationshipRankUpScreenshot : IMaaCustomRecognition
    {
        public string Name { get; set; } = nameof(RelationshipRankUpScreenshot);

        public bool Analyze<T>(T context, in AnalyzeArgs args, in AnalyzeResults results) where T : IMaaContext
        {
            if (!ProgramDataModel.Instance.SettingsData.NoScreenShot && ProgramDataModel.Instance.SettingsData.IsRelationshipRankUpAutoScreenShot)
            {
                if (ScreenShot(context, "RelationshipRankUp"))
                {
                    Utility.PrintLog("好感等级提升，已自动截图");
                    return true;
                }
                else
                {
                    Utility.PrintError("好感等级提升截图失败");
                    return false;
                }
            }
            return true;
        }
    }

    public class InviteUnavailableSkipTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(InviteUnavailableSkipTask);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            Utility.PrintError("咖啡厅邀请失败，邀请券冷却中");
            TaskManager.Instance.CurrentTaskChainPrintFinishedLog = false;
            return true;
        }
    }

    public class InviteScreenshot : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(InviteScreenshot);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            if (!ProgramDataModel.Instance.SettingsData.NoScreenShot)
            {
                if (!ScreenShot(context, "Invite"))
                {
                    Utility.PrintLog("咖啡厅邀请截图失败");
                    return false;
                }

                // 获取所有截图，并按文件名中的日期排序
                var files = Directory.GetFiles(Constants.ScreenshotImageDirectory, "Invite-*.png")
                                     .Select(file => new FileInfo(file)).OrderBy(file => file.Name).ToList();
                // 最多保存10个截图
                const short maxScreenshotCount = 10;
                while (files.Count > maxScreenshotCount)
                {
                    File.Delete(files.First().FullName);
                    files.RemoveAt(0);
                }
            }
            return true;
        }
    }

    public class InviteCancelNotify : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(InviteCancelNotify);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            Utility.PrintLog("规则不符，邀请已取消");
            TaskManager.Instance.CurrentTaskChainPrintFinishedLog = false;
            return true;
        }
    }

    public class DuplicatedLoginStopTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(DuplicatedLoginStopTask);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            Utility.PrintLog("发现重复登录，任务即将停止");
            TaskManager.Instance.Stop(true);
            return true;
        }
    }

    public class MaintenanceStopTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(MaintenanceStopTask);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
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

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            Utility.PrintError("游戏客户端需要更新，任务即将停止。请手动更新后再启动任务");
            TaskManager.Instance.Stop(true);
            return true;
        }
    }

    public class IPBlockedStopTask : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(IPBlockedStopTask);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            Utility.PrintError("登录游戏失败（ip受限）！请开启加速器后再尝试");
            TaskManager.Instance.Stop(true);
            return true;
        }
    }

    public class PrintSweepError : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(PrintSweepError);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            Utility.PrintError($"扫荡关卡H{ProgramDataModel.Instance.SettingsData.HardLevel}失败，体力不足或次数不足");
            TaskManager.Instance.CurrentTaskChainPrintFinishedLog = false;
            return true;
        }
    }

    public class PrintFindeLevelError : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(PrintFindeLevelError);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            Utility.PrintError($"寻找关卡H{ProgramDataModel.Instance.SettingsData.HardLevel}失败，请确认关卡填写正确或关卡已解锁");
            TaskManager.Instance.CurrentTaskChainPrintFinishedLog = false;
            return true;
        }
    }

    public class SweepDropScreenshot : IMaaCustomAction
    {
        public string Name { get; set; } = nameof(SweepDropScreenshot);

        public bool Run<T>(T context, in RunArgs args, in RunResults results) where T : IMaaContext
        {
            if (!ProgramDataModel.Instance.SettingsData.NoScreenShot)
            {
                if (!ScreenShot(context, "Drop"))
                {
                    Utility.PrintLog("扫荡掉落截图失败");
                    return false;
                }

                // 获取所有截图，并按文件名中的日期排序
                var files = Directory.GetFiles(Constants.ScreenshotImageDirectory, "Drop-*.png")
                                     .Select(file => new FileInfo(file)).OrderBy(file => file.Name).ToList();
                // 最多保存10个截图
                const short maxScreenshotCount = 10;
                while (files.Count > maxScreenshotCount)
                {
                    File.Delete(files.First().FullName);
                    files.RemoveAt(0);
                }
            }
            return true;
        }
    }
}
