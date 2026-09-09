namespace Nexora.Models;

public sealed class SystemSnapshot
{
    public string ComputerName { get; init; } = string.Empty;
    public string WindowsVersion { get; init; } = string.Empty;
    public string ProcessorName { get; init; } = string.Empty;
    public double CpuUsagePercent { get; init; }
    public ulong TotalMemoryBytes { get; init; }
    public ulong UsedMemoryBytes { get; init; }
    public ulong AvailableMemoryBytes { get; init; }
    public double MemoryUsagePercent { get; init; }
    public string SystemDriveName { get; init; } = string.Empty;
    public long SystemDriveTotalBytes { get; init; }
    public long SystemDriveFreeBytes { get; init; }
    public double DiskUsagePercent { get; init; }
    public TimeSpan Uptime { get; init; }
}
