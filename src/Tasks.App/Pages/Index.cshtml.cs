using Microsoft.AspNetCore.Mvc.RazorPages;
using Tasks.App.Models;
using Tasks.App.Services;

namespace Tasks.App.Pages;

public class IndexModel(TaskService tasks) : PageModel
{
    public IReadOnlyList<TaskItem> Open { get; private set; } = [];

    public int DoneCount { get; private set; }

    public void OnGet()
    {
        Open = tasks.Open();
        DoneCount = tasks.Completed().Count;
    }
}
