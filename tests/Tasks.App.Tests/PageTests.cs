namespace Tasks.App.Tests;

public class PageTests
{
    [Fact]
    public async Task Home_lists_open_tasks()
    {
        using var app = new TestApp();
        app.Tasks.Add("Write report");
        using var client = app.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("Write report", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Recent_shows_the_latest_task()
    {
        using var app = new TestApp();
        app.Tasks.Add("Older");
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        app.Tasks.Add("Newer");
        using var client = app.CreateClient();

        var html = await client.GetStringAsync("/Recent");

        Assert.Contains("Latest: NEWER", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Recent_api_is_newest_first()
    {
        using var app = new TestApp();
        app.Tasks.Add("Older");
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        app.Tasks.Add("Newer");
        using var client = app.CreateClient();

        var json = await client.GetStringAsync("/api/tasks/recent");

        Assert.True(json.IndexOf("Newer", StringComparison.Ordinal) < json.IndexOf("Older", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Home_shows_latest_activity()
    {
        using var app = new TestApp();
        var task = app.Tasks.Add("Write report");
        app.Tasks.Complete(task.Id);
        using var client = app.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("Completed: Write report", html, StringComparison.Ordinal);
    }
}
