using System.Collections.ObjectModel;
using Nexora.Helpers;
using Nexora.Models;
using Nexora.Services;

namespace Nexora.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SystemDiagnosticsService _systemDiagnosticsService;
    private readonly HealthScoreService _healthScoreService;
    private readonly RecommendationService _recommendationService;
    private int _healthScore;
    private bool _isScanning;
    private string _scanStatus = "Ready to scan your PC.";
    private string _cpuUsageText = "Not scanned yet";
    private string _ramUsageText = "Not scanned yet";
    private string _diskUsageText = "Not scanned yet";
    private string _windowsVersionText = "Not scanned yet";
    private string _uptimeText = "Not scanned yet";

    public MainViewModel(
        SystemDiagnosticsService systemDiagnosticsService,
        HealthScoreService healthScoreService,
        RecommendationService recommendationService)
    {
        _systemDiagnosticsService = systemDiagnosticsService;
        _healthScoreService = healthScoreService;
        _recommendationService = recommendationService;

        Recommendations = new ObservableCollection<RecommendationItem>
        {
            new()
            {
                Title = "Run a scan to begin",
                Message = "Nexora is ready to inspect CPU, memory, disk space, Windows details, and uptime. When you are ready, select Scan My PC to retrieve live system information."
            }
        };

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsScanning);
    }

    public AsyncRelayCommand ScanCommand { get; }

    public ObservableCollection<RecommendationItem> Recommendations { get; }

    public int HealthScore
    {
        get => _healthScore;
        private set
        {
            if (SetProperty(ref _healthScore, value))
            {
                OnPropertyChanged(nameof(HealthScoreText));
                OnPropertyChanged(nameof(HealthSummary));
            }
        }
    }

    public string HealthScoreText => $"{HealthScore:0}";

    public string HealthSummary => HealthScore switch
    {
        >= 85 => "Healthy",
        >= 70 => "Stable",
        >= 50 => "Needs attention",
        > 0 => "Poor",
        _ => "Not scanned"
    };

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(IsReadyToScan));
                ScanCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsReadyToScan => !IsScanning;

    public string ScanStatus
    {
        get => _scanStatus;
        private set => SetProperty(ref _scanStatus, value);
    }

    public string CpuUsageText
    {
        get => _cpuUsageText;
        private set => SetProperty(ref _cpuUsageText, value);
    }

    public string RamUsageText
    {
        get => _ramUsageText;
        private set => SetProperty(ref _ramUsageText, value);
    }

    public string DiskUsageText
    {
        get => _diskUsageText;
        private set => SetProperty(ref _diskUsageText, value);
    }

    public string WindowsVersionText
    {
        get => _windowsVersionText;
        private set => SetProperty(ref _windowsVersionText, value);
    }

    public string UptimeText
    {
        get => _uptimeText;
        private set => SetProperty(ref _uptimeText, value);
    }

    private async Task ScanAsync()
    {
        try
        {
            IsScanning = true;
            ScanStatus = "Starting scan...";

            var progress = new Progress<string>(status => ScanStatus = status);
            var snapshot = await _systemDiagnosticsService.ScanAsync(progress);
            HealthScore = _healthScoreService.Calculate(snapshot);

            CpuUsageText = $"{snapshot.CpuUsagePercent:F1}% current usage";
            RamUsageText = $"{snapshot.MemoryUsagePercent:F1}% used • {FormatBytes(snapshot.UsedMemoryBytes)} / {FormatBytes(snapshot.TotalMemoryBytes)}";
            DiskUsageText = $"{snapshot.DiskUsagePercent:F1}% used • {FormatBytes((ulong)snapshot.SystemDriveFreeBytes)} free on {snapshot.SystemDriveName}";
            WindowsVersionText = $"{snapshot.WindowsVersion}\n{snapshot.ComputerName}\n{snapshot.ProcessorName}";
            UptimeText = FormatUptime(snapshot.Uptime);

            Recommendations.Clear();
            foreach (var recommendation in _recommendationService.Generate(snapshot))
            {
                Recommendations.Add(recommendation);
            }

            ScanStatus = $"Scan complete. Health score calculated from live CPU, memory, and disk data.";
        }
        catch (Exception ex)
        {
            ScanStatus = $"Scan failed: {ex.Message}";
            Recommendations.Clear();
            Recommendations.Add(new RecommendationItem
            {
                Title = "Scan could not complete",
                Message = "Nexora was unable to finish the scan, so the dashboard may be incomplete. Try running the scan again, and if the issue continues, reopen the app with normal Windows permissions and check whether system information access is restricted."
            });
        }
        finally
        {
            IsScanning = false;
        }
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var index = 0;

        while (value >= 1024 && index < suffixes.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return $"{value:F1} {suffixes[index]}";
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        }

        if (uptime.TotalHours >= 1)
        {
            return $"{uptime.Hours}h {uptime.Minutes}m";
        }

        return $"{uptime.Minutes}m";
    }
}
