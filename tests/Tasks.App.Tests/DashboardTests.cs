namespace Tasks.App.Tests;

public class DashboardTests
{
    [Fact]
    public async Task Dashboard_shows_the_completion_rate()
    {
        using var app = new TestApp();
        app.Tasks.Add("Open one");
        var done = app.Tasks.Add("Done one");
        app.Tasks.Complete(done.Id);
        using var client = app.CreateClient();

        var html = await client.GetStringAsync("/Dashboard");

        Assert.Contains("Completed: 50% (1 of 2)", html, StringComparison.Ordinal);
    }
}
