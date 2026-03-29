using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace MaaBATapAssistant.Utils;

public static class Utility
{
    public static void CustomDebugWriteLine(string content)
    {
        Serilog.Log.Information("[WriteLine]  " + content);
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"{DateTime.Now}  {content}");
#endif
    }

    /// <summary>打印输出到程序的主页上</summary>
    public static void PrintLog(string content)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MaaBATapAssistant.ViewModels.MainViewModel.Instance.LogDataList.Add(new($"{DateTime.Now.ToString("MM/dd HH:mm:ss")}   ", content, false));
        });
        Serilog.Log.Information("[PrintLog]   " + content);
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"{DateTime.Now}  PrintLog - {content}");
#endif
    }
    public static void PrintError(string content)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MaaBATapAssistant.ViewModels.MainViewModel.Instance.LogDataList.Add(new($"{DateTime.Now.ToString("MM/dd HH:mm:ss")}   ", content, true));
        });
        Serilog.Log.Error("[PrintError] " + content);
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"{DateTime.Now}  PrintError - {content}");
#endif
    }

    public static void MyGrowlInfo(string content)
    {
        Serilog.Log.Information("[GrowlInfo]  " + content);
        HandyControl.Controls.Growl.Info(content);
    }
    public static void MyGrowlError(string content)
    {
        Serilog.Log.Error("[GrowlError] " + content);
        HandyControl.Controls.Growl.Error(content);
    }
    public static void MyGrowlAsk(string content, Func<bool, bool> funcBeforeClose)
    {
        Serilog.Log.Information("[GrowlAsk]   " + content);
        HandyControl.Controls.Growl.Ask(content, funcBeforeClose);
    }

    // 获取枚举所有值的 Description
    public static List<string> GetEnumDescriptions<T>() where T : Enum
    {
        var descriptions = new List<string>();
        var values = Enum.GetValues(typeof(T));

        foreach (var value in values)
        {
            var field = typeof(T).GetField(value.ToString() ?? string.Empty);
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            if (attribute != null)
            {
                descriptions.Add(attribute.Description);
            }
            else
            {
                descriptions.Add(value.ToString() ?? string.Empty);
            }
        }
        return descriptions;
    }
}
