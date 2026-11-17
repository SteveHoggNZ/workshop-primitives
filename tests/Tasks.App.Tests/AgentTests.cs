using Microsoft.Extensions.DependencyInjection;
using Tasks.App.Agent;

namespace Tasks.App.Tests;

public class AgentTests
{
    [Fact]
    public void Help_lists_the_commands()
    {
        using var app = new TestApp();
        var reply = Agent(app).Handle("help");

        Assert.Contains("add <title>", reply, StringComparison.Ordinal);
        Assert.Contains("delete <id>", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void Add_then_list()
    {
        using var app = new TestApp();
        var agent = Agent(app);

        Assert.StartsWith("Added task", agent.Handle("add buy milk"), StringComparison.Ordinal);
        Assert.Contains("buy milk", agent.Handle("list"), StringComparison.Ordinal);
    }

    [Fact]
    public void Delete_asks_for_confirmation()
    {
        using var app = new TestApp();
        var task = app.Tasks.Add("Old");
        var agent = Agent(app);

        Assert.Contains("Type yes to confirm", agent.Handle($"delete {task.Id}"), StringComparison.Ordinal);
        Assert.Equal($"Deleted task {task.Id}.", agent.Handle("yes"));
        Assert.Empty(app.Tasks.List());
    }

    [Fact]
    public void Delete_cancelled_by_no()
    {
        using var app = new TestApp();
        var task = app.Tasks.Add("Keep");
        var agent = Agent(app);

        agent.Handle($"delete {task.Id}");

        Assert.Equal("Cancelled.", agent.Handle("no"));
        Assert.Single(app.Tasks.List());
    }

    [Fact]
    public void Unknown_command()
    {
        using var app = new TestApp();
        Assert.Contains("don't understand", Agent(app).Handle("dance"), StringComparison.Ordinal);
    }

    private static ScriptedAgent Agent(TestApp app) => app.Services.GetRequiredService<ScriptedAgent>();
}
