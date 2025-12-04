using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using GameForum.Services;

namespace GameForum.Pages;

[Authorize]
public class StatsModel : PageModel
{
    private readonly StatsService _stats;
    public StatsModel(StatsService stats) { _stats = stats; }

    public string LineChart { get; private set; } = string.Empty;
    public string BarChart { get; private set; } = string.Empty;
    public string PieChart { get; private set; } = string.Empty;

    public void OnGet()
    {
        var result = _stats.Generate();
        LineChart = result.LineChartPath;
        BarChart = result.BarChartPath;
        PieChart = result.PieChartPath;
    }
}
