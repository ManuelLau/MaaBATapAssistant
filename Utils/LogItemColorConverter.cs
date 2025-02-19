using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MaaBATapAssistant.Utils;

public class LogItemColorConverter : IValueConverter
{
    public object Convert(object isRed, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)isRed ? Brushes.Red : Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
