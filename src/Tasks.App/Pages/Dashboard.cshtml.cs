using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tasks.App.Services;

namespace Tasks.App.Pages;

public class DashboardModel(TaskService tasks, TimeProvider clock, AppSettings settings) : PageModel
{
    public int Total { get; private set; }

    public int Done { get; private set; }

    public int Percent { get; private set; }

    public string WeekStarting { get; private set; } = "";

    public void OnGet()
    {
        var all = tasks.List();
        Total = all.Count;
        Done = all.Count(t => t.Done);
        Percent = TaskStats.CompletionPercent(Done, Total);
        WeekStarting = TaskStats.WeekStart(clock.GetUtcNow(), settings.Zone).ToString("d MMM", CultureInfo.InvariantCulture);
    }
}
