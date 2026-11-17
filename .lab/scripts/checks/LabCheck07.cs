// Hidden scorer checks for Step 7. Copied into the clone's test project at scoring time, then removed.
using System.Net;

namespace Tasks.App.Tests;

public class LabCheck07
{
    [Fact]
    public async Task Archive_lists_completed_tasks_oldest_first()
    {
        using var app = new TestApp();
        var open = app.Tasks.Add("Still open");
        var second = app.Tasks.Add("Finished second"); // created first, completed second: oldest by completion, not creation
        var first = app.Tasks.Add("Finished first");
        app.Tasks.Complete(first.Id);
        app.Clock.Advance(TimeSpan.FromMinutes(5));
        app.Tasks.Complete(second.Id);
        using var client = app.CreateClient();

        var response = await client.GetAsync("/Archive");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var a = html.IndexOf("Finished first", StringComparison.Ordinal);
        var b = html.IndexOf("Finished second", StringComparison.Ordinal);
        Assert.True(a >= 0 && b > a, "completed tasks, oldest first");
        Assert.DoesNotContain(open.Title, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Archive_works_with_no_tasks()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/Archive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
