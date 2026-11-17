using Tasks.App.Models;
using Tasks.App.Services;

namespace Tasks.App;

/// <summary>The JSON API for tasks, under /api/tasks.</summary>
public static class TaskApi
{
    public sealed record NewTask(string? Title);

    public sealed record TaskChange(string? Title, bool? Done);

    public static void Map(WebApplication app)
    {
        app.MapGet("/api/tasks/recent", (ITaskQueries queries) => queries.Recent());

        app.MapPost("/api/tasks", (NewTask body, TaskService tasks) =>
        {
            if (string.IsNullOrWhiteSpace(body.Title))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["A task needs a title."] });
            }

            var task = tasks.Add(body.Title);
            return Results.Created($"/api/tasks/{task.Id}", task);
        });

        app.MapPatch("/api/tasks/{id}", (string id, TaskChange body, TaskService tasks) =>
        {
            var task = tasks.List().FirstOrDefault(t => t.Id == id);
            if (task is null)
            {
                return Results.NotFound();
            }

            if (body.Title is { } title && !string.IsNullOrWhiteSpace(title))
            {
                task = tasks.Rename(id, title) ?? task;
            }

            if (body.Done == true)
            {
                task = tasks.Complete(id) ?? task;
            }

            return Results.Ok(task);
        });
    }
}
