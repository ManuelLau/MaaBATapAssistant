using Serilog;
using System.Windows;

namespace MaaBATapAssistant;

public partial class App : Application
{
    public App()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Utils.Constants.LogFilePath, 
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 10)
            .CreateLogger();
    }
}
