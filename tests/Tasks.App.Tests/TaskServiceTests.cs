namespace Tasks.App.Tests;

public class TaskServiceTests
{
    [Fact]
    public void Complete_records_the_time()
    {
        using var app = new TestApp();
        var task = app.Tasks.Add("Write report");
        app.Clock.Advance(TimeSpan.FromMinutes(30));

        var done = app.Tasks.Complete(task.Id);

        Assert.NotNull(done);
        Assert.Equal(TestApp.Start.AddMinutes(30), done.CompletedAt);
    }

    [Fact]
    public void Recent_is_newest_first()
    {
        using var app = new TestApp();
        app.Tasks.Add("First");
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        app.Tasks.Add("Second");

        Assert.Equal(new[] { "Second", "First" }, app.Tasks.Recent().Select(t => t.Title));
    }
}
