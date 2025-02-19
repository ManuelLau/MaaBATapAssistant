namespace MaaBATapAssistant.Models;

public class MainViewLogItemModel(string dateTimeString, string content, bool isRed)
{
    public string DateTime { get; set; } = dateTimeString;
    public string Content { get; set; } = content;
    public bool IsRed { get; set; } = isRed;
}
