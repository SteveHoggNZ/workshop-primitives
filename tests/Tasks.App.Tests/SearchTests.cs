namespace Tasks.App.Tests;

public class SearchTests
{
    [Fact]
    public async Task Bulk_search_ignores_case_and_stop_words()
    {
        using var app = new TestApp();
        app.Tasks.Add("Write the report for the team");
        app.Tasks.Add("Book a room");
        using var client = app.CreateClient();

        var html = await client.GetStringAsync("/Bulk?q=WRITE%20report%20team");

        Assert.Contains("1 matching", html, StringComparison.Ordinal);
    }
}
