using System.Windows.Controls;
using System.Windows;

namespace MaaBATapAssistant.Utils;

public class AutoScrollToBottom
{
    public static readonly DependencyProperty AutoScrollToBottomProperty = DependencyProperty.RegisterAttached("AlwaysScrollToEnd", typeof(bool), typeof(AutoScrollToBottom), new PropertyMetadata(false, AlwaysScrollToBottomChanged));
    private static bool _autoScroll;

    private static void AlwaysScrollToBottomChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ScrollViewer? scroll = sender as ScrollViewer;
        if (scroll != null)
        {
            bool alwaysScrollToEnd = (e.NewValue != null) && (bool)e.NewValue;
            if (alwaysScrollToEnd)
            {
                scroll.ScrollToEnd();
                scroll.ScrollChanged += ScrollChanged;
            }
            else { scroll.ScrollChanged -= ScrollChanged;}
        }
        else { throw new InvalidOperationException("The attached AlwaysScrollToEnd property can only be applied to ScrollViewer instances."); }
    }

    public static bool GetAlwaysScrollToBottom(ScrollViewer scroll)
    {
        if (scroll == null) { throw new ArgumentNullException("scroll"); }
        return (bool)scroll.GetValue(AutoScrollToBottomProperty);
    }

    public static void SetAlwaysScrollToBottom(ScrollViewer scroll, bool alwaysScrollToEnd)
    {
        if (scroll == null) { throw new ArgumentNullException("scroll"); }
        scroll.SetValue(AutoScrollToBottomProperty, alwaysScrollToEnd);
    }

    private static void ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        ScrollViewer? scroll = sender as ScrollViewer;
        if (scroll == null) { throw new InvalidOperationException("The attached AlwaysScrollToEnd property can only be applied to ScrollViewer instances."); }
        if (e.ExtentHeightChange == 0) { _autoScroll = scroll.VerticalOffset == scroll.ScrollableHeight; }
        if (_autoScroll && e.ExtentHeightChange != 0) { scroll.ScrollToVerticalOffset(scroll.ExtentHeight); }
    }
}