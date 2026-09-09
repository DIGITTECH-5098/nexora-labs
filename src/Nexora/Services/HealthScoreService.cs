using Nexora.Models;

namespace Nexora.Services;

public sealed class HealthScoreService
{
    public int Calculate(SystemSnapshot snapshot)
    {
        var score = 100;

        score -= snapshot.MemoryUsagePercent switch
        {
            >= 90 => 30,
            >= 80 => 20,
            >= 70 => 10,
            _ => 0
        };

        score -= snapshot.DiskUsagePercent switch
        {
            >= 95 => 30,
            >= 85 => 20,
            >= 75 => 10,
            _ => 0
        };

        score -= snapshot.CpuUsagePercent switch
        {
            >= 90 => 20,
            >= 75 => 12,
            >= 60 => 6,
            _ => 0
        };

        return Math.Clamp(score, 0, 100);
    }
}
