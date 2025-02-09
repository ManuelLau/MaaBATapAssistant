namespace MaaBATapAssistant.Utils;

public static class Utility
{
#if DEBUG
    public static void DebugWriteLine(string content)
    {
        System.Diagnostics.Debug.WriteLine($"{DateTime.Now}  {content}");
    }
#endif
}
