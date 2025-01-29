using System.Globalization;
using System.Windows.Data;
using MaaBATapAssistant.Models;

namespace MaaBATapAssistant.Utils;

// 根据任务状态的不同返回不同的颜色
class TaskStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        switch ((ETaskChainStatus)value)
        {
            case ETaskChainStatus.Waiting:
                return new string("#E0E0E0");//浅绿90EE90
            case ETaskChainStatus.Running:
                return new string("#00F268");
            default:
                return new string("#E0E0E0");//灰色E0E0E0
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
