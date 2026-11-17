using Tasks.App.Data;
using Tasks.App.Models;

namespace Tasks.App.Services;

/// <summary>The only way the rest of the app reads or changes tasks.</summary>
public sealed class TaskService(TaskStore store, TimeProvider clock) : ITaskQueries
{
    public TaskItem Add(string title)
    {
        var task = new TaskItem(Guid.NewGuid().ToString("N")[..8], title.Trim(), clock.GetUtcNow());
        store.Add(task);
        return task;
    }

    public TaskItem? Complete(string id)
    {
        var task = store.All().FirstOrDefault(t => t.Id == id);
        if (task is null || task.Done)
        {
            return task;
        }

        var done = task with { CompletedAt = clock.GetUtcNow() };
        store.Replace(done);
        return done;
    }

    public IReadOnlyList<TaskItem> List() => store.All();

    public IReadOnlyList<TaskItem> Open() => [.. store.All().Where(t => !t.Done).OrderBy(t => t.CreatedAt)];

    public IReadOnlyList<TaskItem> Recent(int count = 5) =>
        [.. store.All().OrderByDescending(t => t.CreatedAt).Take(count)];

    public IReadOnlyList<TaskItem> Completed() =>
        [.. store.All().Where(t => t.Done).OrderByDescending(t => t.CompletedAt)];

    public void DeleteAll() => store.Clear();
}
