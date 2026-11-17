namespace Tasks.App.Services;

/// <summary>Short "what just happened" lines for the home page.</summary>
public sealed class ActivityFeed(TaskService tasks)
{
    public IReadOnlyList<string> Recent(int count = 3) =>
        [.. tasks.Open().Take(count).Select(t => "Completed: " + t.Title)];
}
