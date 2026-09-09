using Microsoft.UI.Xaml;
using Nexora.Services;
using Nexora.ViewModels;
using Nexora.Views;

namespace Nexora;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var diagnosticsService = new SystemDiagnosticsService();
        var healthScoreService = new HealthScoreService();
        var recommendationService = new RecommendationService();
        var mainViewModel = new MainViewModel(diagnosticsService, healthScoreService, recommendationService);

        _window = new MainWindow(mainViewModel);
        _window.Activate();
    }
}
