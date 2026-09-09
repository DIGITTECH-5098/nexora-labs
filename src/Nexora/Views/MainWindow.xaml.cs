using Microsoft.UI.Xaml;
using Nexora.ViewModels;

namespace Nexora.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        Title = "Nexora";
        DataContext = viewModel;
        Width = 1440;
        Height = 920;
    }
}
