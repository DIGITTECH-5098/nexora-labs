using System.Runtime.InteropServices;
using Microsoft.Win32;
using Nexora.Models;

namespace Nexora.Services;

public sealed class SystemDiagnosticsService
{
    public async Task<SystemSnapshot> ScanAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("Sampling CPU usage...");
        var cpuUsage = await GetCpuUsageAsync(cancellationToken);

        progress?.Report("Reading memory usage...");
        var memoryInfo = GetMemoryInfo();

        progress?.Report("Checking system drive...");
        var systemDrive = GetSystemDriveInfo();

        progress?.Report("Reading Windows details...");
        var windowsVersion = GetWindowsVersion();
        var processorName = GetProcessorName();
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

        return new SystemSnapshot
        {
            ComputerName = Environment.MachineName,
            WindowsVersion = windowsVersion,
            ProcessorName = processorName,
            CpuUsagePercent = cpuUsage,
            TotalMemoryBytes = memoryInfo.TotalPhysicalMemory,
            UsedMemoryBytes = memoryInfo.UsedPhysicalMemory,
            AvailableMemoryBytes = memoryInfo.AvailablePhysicalMemory,
            MemoryUsagePercent = memoryInfo.UsagePercent,
            SystemDriveName = systemDrive.Name,
            SystemDriveTotalBytes = systemDrive.TotalBytes,
            SystemDriveFreeBytes = systemDrive.FreeBytes,
            DiskUsagePercent = systemDrive.UsagePercent,
            Uptime = uptime
        };
    }

    private static async Task<double> GetCpuUsageAsync(CancellationToken cancellationToken)
    {
        var first = CpuTimesSnapshot.Create();
        await Task.Delay(TimeSpan.FromMilliseconds(900), cancellationToken);
        var second = CpuTimesSnapshot.Create();
        return CpuTimesSnapshot.CalculateUsagePercent(first, second);
    }

    private static MemoryInfo GetMemoryInfo()
    {
        var status = new MemoryStatusEx();
        status.dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();

        if (!GlobalMemoryStatusEx(ref status))
        {
            throw new InvalidOperationException("Unable to read memory statistics from Windows.");
        }

        var usedMemory = status.ullTotalPhys - status.ullAvailPhys;
        var usagePercent = status.ullTotalPhys == 0
            ? 0
            : usedMemory / (double)status.ullTotalPhys * 100d;

        return new MemoryInfo(status.ullTotalPhys, usedMemory, status.ullAvailPhys, usagePercent);
    }

    private static DriveUsageInfo GetSystemDriveInfo()
    {
        var systemDirectory = Environment.SystemDirectory;
        var systemDriveRoot = Path.GetPathRoot(systemDirectory);

        if (string.IsNullOrWhiteSpace(systemDriveRoot))
        {
            throw new InvalidOperationException("Unable to determine the Windows system drive.");
        }

        var drive = new DriveInfo(systemDriveRoot);
        var usedBytes = drive.TotalSize - drive.AvailableFreeSpace;
        var usagePercent = drive.TotalSize == 0
            ? 0
            : usedBytes / (double)drive.TotalSize * 100d;

        return new DriveUsageInfo(drive.Name, drive.TotalSize, drive.AvailableFreeSpace, usagePercent);
    }

    private static string GetWindowsVersion()
    {
        return RuntimeInformation.OSDescription.Trim();
    }

    private static string GetProcessorName()
    {
        return Registry.GetValue(
                   @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                   "ProcessorNameString",
                   null) as string
               ?? Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
               ?? "Processor name unavailable";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint DwLowDateTime;
        public uint DwHighDateTime;

        public ulong ToUInt64()
        {
            return ((ulong)DwHighDateTime << 32) | DwLowDateTime;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    private readonly record struct MemoryInfo(ulong TotalPhysicalMemory, ulong UsedPhysicalMemory, ulong AvailablePhysicalMemory, double UsagePercent);

    private readonly record struct DriveUsageInfo(string Name, long TotalBytes, long FreeBytes, double UsagePercent);

    private readonly record struct CpuTimesSnapshot(ulong IdleTime, ulong KernelTime, ulong UserTime)
    {
        public static CpuTimesSnapshot Create()
        {
            if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            {
                throw new InvalidOperationException("Unable to read CPU usage from Windows.");
            }

            return new CpuTimesSnapshot(idleTime.ToUInt64(), kernelTime.ToUInt64(), userTime.ToUInt64());
        }

        public static double CalculateUsagePercent(CpuTimesSnapshot first, CpuTimesSnapshot second)
        {
            var idle = second.IdleTime - first.IdleTime;
            var kernel = second.KernelTime - first.KernelTime;
            var user = second.UserTime - first.UserTime;
            var total = kernel + user;

            if (total == 0 || total <= idle)
            {
                return 0;
            }

            var active = total - idle;
            return Math.Clamp(active / (double)total * 100d, 0d, 100d);
        }
    }
}
