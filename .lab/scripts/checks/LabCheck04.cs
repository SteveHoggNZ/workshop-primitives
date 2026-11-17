// Hidden scorer checks for Step 4. Copied into the clone's test project at scoring time, then removed.
// They go through the page, so they hold however the fix is structured.
using System.Globalization;
using System.Net;

namespace Tasks.App.Tests;

public class LabCheck04
{
    private static async Task<(HttpStatusCode Status, string Html)> Dashboard(string? now, bool withTasks)
    {
        using var app = new TestApp();
        if (now is not null)
        {
            app.Clock.Set(DateTimeOffset.Parse(now, CultureInfo.InvariantCulture));
        }

        if (withTasks)
        {
            app.Tasks.Add("open");
            app.Tasks.Complete(app.Tasks.Add("done").Id);
        }

        using var client = app.CreateClient();
        var response = await client.GetAsync("/Dashboard");
        return (response.StatusCode, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Empty_dashboard_shows_zero_percent()
    {
        var (status, html) = await Dashboard(null, withTasks: false);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Matches(@"\b0\s?%", html);
    }

    [Theory]
    [InlineData("2026-11-16T08:00:00+13:00")] // Monday morning in Auckland: still Sunday in UTC
    [InlineData("2026-11-16T00:30:00+13:00")]
    [InlineData("2026-11-22T23:30:00+13:00")] // Sunday night: same week
    public async Task Week_starts_on_the_local_monday(string now)
    {
        var (status, html) = await Dashboard(now, withTasks: true);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("16 Nov", html, StringComparison.Ordinal);
        Assert.DoesNotContain("9 Nov", html, StringComparison.Ordinal);
    }
}
