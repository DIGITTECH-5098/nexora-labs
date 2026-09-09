using Nexora.Models;

namespace Nexora.Services;

public sealed class RecommendationService
{
    public IReadOnlyList<RecommendationItem> Generate(SystemSnapshot snapshot)
    {
        var recommendations = new List<RecommendationItem>();

        if (snapshot.MemoryUsagePercent >= 80)
        {
            recommendations.Add(new RecommendationItem
            {
                Title = "High memory usage detected",
                Message = "Nexora detected that memory usage is high, which can slow app switching and overall responsiveness. Closing unnecessary applications or browser tabs is a safe first step to free RAM and reduce paging."
            });
        }

        if (snapshot.DiskUsagePercent >= 85)
        {
            recommendations.Add(new RecommendationItem
            {
                Title = "System drive is running low on space",
                Message = "Nexora detected that the Windows system drive is nearly full, which can reduce room for updates, temporary files, and virtual memory. Safely remove unneeded personal files or uninstall unused apps to recover space without changing core system settings."
            });
        }

        if (snapshot.CpuUsagePercent >= 75)
        {
            recommendations.Add(new RecommendationItem
            {
                Title = "CPU usage is elevated",
                Message = "Nexora detected sustained CPU activity during the scan, which can make the PC feel hot, loud, or sluggish. Open Task Manager to see which applications are using the most processing power and close only the ones you recognize and do not need."
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new RecommendationItem
            {
                Title = "No major performance problems detected",
                Message = "Nexora did not detect a major CPU, memory, or system drive issue during this scan, which suggests the PC is currently in healthy shape. Keep Windows updated, leave free disk space available, and re-run a scan if the computer starts to feel slow again."
            });
        }

        return recommendations;
    }
}
