using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tasks.App.Tests;

public class ApiTests
{
    [Fact]
    public async Task Create_then_complete_a_task()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();

        var created = await client.PostAsJsonAsync("/api/tasks", new { title = "Write tests" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var done = await client.PatchAsJsonAsync($"/api/tasks/{id}", new { done = true });

        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.True((await done.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("done").GetBoolean());
    }

    [Fact]
    public async Task A_task_needs_a_title()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
