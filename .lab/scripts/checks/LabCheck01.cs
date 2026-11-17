// Hidden scorer checks for Step 1. Copied into the clone's test project at scoring time, then removed.
using System.Net;

namespace Tasks.App.Tests;

public class LabCheck01
{
    private static async Task<(HttpStatusCode Status, string Html, string Id)> Get(bool withTask)
    {
        using var app = new TestApp();
        var id = "";
        if (withTask)
        {
            var task = app.Tasks.Add("Checked task");
            app.Tasks.Complete(task.Id);
            id = task.Id;
        }

        using var client = app.CreateClient();
        var response = await client.GetAsync("/Completed");
        return (response.StatusCode, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()), id);
    }

    [Fact]
    public async Task Route_returns_200()
    {
        var (status, html, _) = await Get(withTask: true);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("Checked task", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Load_time_has_the_offset()
    {
        var (_, html, _) = await Get(withTask: true);
        Assert.Contains("10:00 +13:00", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Template_conventions()
    {
        var (_, html, id) = await Get(withTask: true);
        Assert.Contains("data-page=\"completed\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"task-row\"", html, StringComparison.Ordinal);
        Assert.Contains($"data-task-id=\"{id}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Empty_state_uses_partial()
    {
        var (status, html, _) = await Get(withTask: false);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("Nothing here yet.", html, StringComparison.Ordinal);
    }
}
