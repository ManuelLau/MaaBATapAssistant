using MaaBATapAssistant.ViewModels;

namespace MaaBATapAssistant.Views;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainViewModel.Instance;
    }
}