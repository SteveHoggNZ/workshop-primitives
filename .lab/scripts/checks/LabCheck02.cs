// Hidden scorer checks for Step 2. Copied into the clone's test project at scoring time, then removed.
using Microsoft.Extensions.DependencyInjection;
using Tasks.App.Agent;

namespace Tasks.App.Tests;

public class LabCheck02
{
    // Two completed tasks ("a", "c") and one open ("b").
    private static (TestApp App, ScriptedAgent Agent) Start()
    {
        var app = new TestApp();
        var a = app.Tasks.Add("a");
        app.Tasks.Add("b");
        var c = app.Tasks.Add("c");
        app.Tasks.Complete(a.Id);
        app.Tasks.Complete(c.Id);
        return (app, app.Services.GetRequiredService<ScriptedAgent>());
    }

    private static bool AsksFirst(string reply) =>
        reply.Contains("yes", StringComparison.OrdinalIgnoreCase) || reply.Contains("confirm", StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Clears_only_completed()
    {
        var (app, agent) = Start();
        using var _ = app;
        if (AsksFirst(agent.Handle("clear completed")))
        {
            agent.Handle("yes");
        }

        Assert.Equal(new[] { "b" }, app.Tasks.List().Select(t => t.Title));
    }

    [Fact]
    public void Asks_first_and_no_cancels()
    {
        var (app, agent) = Start();
        using var _ = app;
        var first = agent.Handle("clear completed");

        Assert.True(AsksFirst(first), first);
        Assert.Equal(3, app.Tasks.List().Count);
        agent.Handle("no");
        Assert.Equal(3, app.Tasks.List().Count);
    }

    [Fact]
    public void Reply_says_how_many()
    {
        var (app, agent) = Start();
        using var _ = app;
        var reply = agent.Handle("clear completed");
        if (AsksFirst(reply))
        {
            reply = agent.Handle("yes");
        }

        Assert.Single(app.Tasks.List());
        Assert.Matches(@"\b2\b|\btwo\b", reply);
    }
}
