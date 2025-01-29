using System.Runtime.InteropServices;

namespace MaaBATapAssistant.Utils;

//执行挂机任务时，阻止系统睡眠。不执行挂机后，恢复默认设置
public class PreventSleepTool
{
    private const uint ES_CONTINUOUS = 0x80000000;
    private const int ES_SYSTEM_REQUIRED = 0x00000001;
    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint flags);

    public static void PreventSleep()
    {
        SetThreadExecutionState(ES_SYSTEM_REQUIRED);
    }

    public static void RestoreSleep()
    {
        SetThreadExecutionState(ES_CONTINUOUS);
    }
}
