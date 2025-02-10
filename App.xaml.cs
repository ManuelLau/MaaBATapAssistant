using Serilog;
using System.Windows;

namespace MaaBATapAssistant;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(MaaBATapAssistant.Utils.MyConstant.LogFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}
