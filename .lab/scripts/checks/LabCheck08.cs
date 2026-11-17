// Hidden acceptance tests for Step 8 (backlog item B1). Copied into the clone's test project at scoring time.
// The contract is the stakeholder's (.lab/facilitator/step08/answer-key.md): an agent only knows it by asking.
// TestApp's clock: Monday 16 Nov 2026, 10:00 in Auckland (Sunday 15 Nov 21:00 UTC).
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tasks.App.Tests;

public class LabCheck08
{
    private static async Task<JsonElement> Make(HttpClient client, string title, string? priority = null, string? dueDate = null)
    {
        var body = new Dictionary<string, object?> { ["title"] = title };
        if (priority is not null)
        {
            body["priority"] = priority;
        }

        if (dueDate is not null)
        {
            body["dueDate"] = dueDate;
        }

        var r = await client.PostAsJsonAsync("/api/tasks", body);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<List<string>> Titles(HttpClient client, string query)
    {
        var rows = await client.GetFromJsonAsync<JsonElement>("/api/tasks" + query);
        return [.. rows.EnumerateArray().Select(t => t.GetProperty("title").GetString()!)];
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    [Fact]
    public async Task B1_1_defaults()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        var t = await Make(client, "plain");

        Assert.Equal("normal", Str(t, "priority"));
        Assert.True(t.TryGetProperty("dueDate", out var due) && due.ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task B1_2_create_with_priority_and_due_date()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        var t = await Make(client, "x", "urgent", "2026-11-20");

        Assert.Equal("urgent", Str(t, "priority"));
        Assert.Equal("2026-11-20", Str(t, "dueDate"));
    }

    [Fact]
    public async Task B1_3_sort_by_priority_with_tie_breaks()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        await Make(client, "A", "low");
        await Make(client, "B", "urgent", "2026-11-25");
        await Make(client, "C", "urgent", "2026-11-18");
        await Make(client, "D", "high");
        await Make(client, "E", "urgent");

        Assert.Equal(["C", "B", "E", "D", "A"], await Titles(client, "?sort=priority"));
    }

    [Fact]
    public async Task B1_4_filter_by_priority()  // newest first, like the list
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        await Make(client, "h1", "high");
        app.Clock.Advance(TimeSpan.FromSeconds(1));
        await Make(client, "n1");
        app.Clock.Advance(TimeSpan.FromSeconds(1));
        await Make(client, "h2", "high");

        Assert.Equal(["h2", "h1"], await Titles(client, "?priority=high"));
    }

    [Fact]
    public async Task B1_5_due_before_is_inclusive_and_skips_undated()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        await Make(client, "early", dueDate: "2026-11-10");
        app.Clock.Advance(TimeSpan.FromSeconds(1));
        await Make(client, "edge", dueDate: "2026-11-20");
        app.Clock.Advance(TimeSpan.FromSeconds(1));
        await Make(client, "late", dueDate: "2026-11-21");
        await Make(client, "none");

        Assert.Equal(["edge", "early"], await Titles(client, "?due_before=2026-11-20"));
    }

    [Fact]
    public async Task B1_6_overdue_uses_local_today()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        await Make(client, "yesterday", dueDate: "2026-11-15");
        await Make(client, "today", dueDate: "2026-11-16");
        var done = await Make(client, "done", dueDate: "2026-11-15");
        await client.PatchAsJsonAsync($"/api/tasks/{done.GetProperty("id").GetString()}", new { done = true });

        var rows = (await client.GetFromJsonAsync<JsonElement>("/api/tasks")).EnumerateArray()
            .ToDictionary(t => t.GetProperty("title").GetString()!, t => t);
        Assert.True(rows["yesterday"].GetProperty("overdue").GetBoolean());
        Assert.False(rows["today"].GetProperty("overdue").GetBoolean());
        Assert.False(rows["done"].GetProperty("overdue").GetBoolean());
    }

    [Fact]
    public async Task B1_7_patch_clears_due_date_and_rejects_unknown_priority()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        var t = await Make(client, "x", dueDate: "2026-11-20");
        var id = t.GetProperty("id").GetString();

        var cleared = await client.PatchAsync($"/api/tasks/{id}", JsonContent.Create(new Dictionary<string, object?> { ["dueDate"] = null }));
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var body = await cleared.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("dueDate", out var due) && due.ValueKind == JsonValueKind.Null);

        var bad = await client.PatchAsJsonAsync($"/api/tasks/{id}", new { priority = "critical" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);
    }

    [Fact]
    public async Task B1_8_task_list_page_shows_priority_and_overdue()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        await Make(client, "Pay invoice", "urgent", "2026-11-15");
        await Make(client, "Water plants", "low", "2026-11-30");

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        // The <li> (or table row) that holds the title, whatever order its parts are in.
        string Row(string title)
        {
            var i = html.IndexOf(title, StringComparison.Ordinal);
            Assert.True(i >= 0, title);
            var start = Math.Max(html.LastIndexOf("<li", i, StringComparison.Ordinal), html.LastIndexOf("<tr", i, StringComparison.Ordinal));
            var ends = new[] { html.IndexOf("</li>", i, StringComparison.Ordinal), html.IndexOf("</tr>", i, StringComparison.Ordinal) }.Where(e => e >= 0);
            var end = ends.Any() ? ends.Min() : Math.Min(html.Length, i + 600);
            return html[Math.Max(0, start)..end];
        }

        Assert.Contains("urgent", Row("Pay invoice"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Overdue", Row("Pay invoice"), StringComparison.Ordinal);
        Assert.DoesNotContain("Overdue", Row("Water plants"), StringComparison.Ordinal);
    }
}
