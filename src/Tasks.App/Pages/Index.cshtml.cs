using Microsoft.AspNetCore.Mvc.RazorPages;
using Tasks.App.Models;
using Tasks.App.Services;

namespace Tasks.App.Pages;

public class IndexModel(TaskService tasks, ActivityFeed feed) : PageModel
{
    public IReadOnlyList<TaskItem> Open { get; private set; } = [];

    public int DoneCount { get; private set; }

    public IReadOnlyList<string> Activity { get; private set; } = [];

    public void OnGet()
    {
        Open = tasks.Open();
        DoneCount = tasks.Completed().Count;
        Activity = feed.Recent(3);
    }
}
