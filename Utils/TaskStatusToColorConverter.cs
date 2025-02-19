using System.Globalization;
using System.Windows.Data;
using MaaBATapAssistant.Models;

namespace MaaBATapAssistant.Utils;

// 根据任务状态的不同返回不同的颜色
public class TaskStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (ETaskChainStatus)value switch
        {
            ETaskChainStatus.Waiting => new string("#E0E0E0"),//灰色E0E0E0
            ETaskChainStatus.InCurrentQueue => new string("#00BFFF"),//道奇蓝#1e90ff
            ETaskChainStatus.Running => new string("#00F268"),//绿
            _ => new string("#E0E0E0"),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
