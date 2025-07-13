using MaaBATapAssistant.ViewModels;
using HandyControl.Controls;
using MaaBATapAssistant.Utils;

namespace MaaBATapAssistant.Views;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainViewModel.Instance;
        MainViewModel.Instance.AppStart();
        Closing += MainViewModel.Instance.AppClosing;
        Utility.MyDebugWriteLine("启动程序");
#if DEBUG
        CheckVersionIsSame();
#endif
    }

    // 屏蔽ScrollViewer的滚动事件
    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        ScrollViewer scrollViewer = (ScrollViewer)sender;
        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    public void DeleteTaskChain(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button)
        {
            if (button.DataContext is MaaBATapAssistant.Models.TaskChainModel item)
                MainViewModel.Instance.WaitingTaskList.Remove(item);
        }
    }

    private void TimeTextBoxLostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        System.Windows.Controls.TextBox? textBox = sender as System.Windows.Controls.TextBox;
        string? text = textBox?.Text;
        bool isSuccess = DateTime.TryParse(text, out DateTime parsedDate);
        if (isSuccess)
        {
            TaskManager.Instance.RefreshWaitingTaskChainDateTime(parsedDate, true);
        }
    }

#if DEBUG
    private static void CheckVersionIsSame()
    {
        Version? version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (version == null)
        {
            throw new Exception("版本号为空！");
        }
        else
        {
            Version myConstantVersion = new(Utils.MyConstant.AppVersion + ".0");
            if (version.Major == myConstantVersion.Major && version.Minor == myConstantVersion.Minor && version.Build == myConstantVersion.Build
                == false)
            {
                throw new Exception("软件版本号前3位不一致！");
            }
        }
    }
#endif
}