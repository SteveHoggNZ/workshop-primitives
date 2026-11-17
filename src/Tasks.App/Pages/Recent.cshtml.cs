using Microsoft.AspNetCore.Mvc.RazorPages;
using Tasks.App.Data;
using Tasks.App.Models;

namespace Tasks.App.Pages;

public class RecentModel : PageModel
{
    private readonly TaskStore _store;

    public RecentModel(TaskStore store)
    {
        _store = store;
    }

    public List<TaskItem> Tasks { get; set; }

    public List<string> Labels { get; set; }

    public int Total { get; set; }

    public string LatestTitle { get; set; } = "";

    public string LoadedAt { get; set; } = "";

    public void OnGet()
    {
        var all = _store.All();
        Total = all.Count();
        Tasks = all.OrderByDescending(t => t.CreatedAt).Take(5).ToList();
        Labels = new List<string>();
        foreach (var task in Tasks)
        {
            Labels.Add(Describe(task));
        }

        LatestTitle = Tasks.First().Title.ToUpper();
        LoadedAt = DateTime.Now.ToString("HH:mm:ss");
    }

    private string Describe(TaskItem task)
    {
        return task.Done ? "done" : "open";
    }
}
