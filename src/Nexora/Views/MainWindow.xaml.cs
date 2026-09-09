using Microsoft.UI.Xaml;
using Nexora.ViewModels;

namespace Nexora.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        Title = "Nexora";

        if (Content is FrameworkElement root)
        {
            root.DataContext = viewModel;
        }
    }
}
