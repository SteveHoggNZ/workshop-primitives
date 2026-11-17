using Tasks.App.Models;

namespace Tasks.App.Data;

/// <summary>
/// In-memory storage. Pages should not use this directly: go through <see cref="Services.TaskService"/>.
/// </summary>
public sealed class TaskStore
{
    private readonly List<TaskItem> _items = [];
    private readonly Lock _gate = new();

    public IReadOnlyList<TaskItem> All()
    {
        lock (_gate)
        {
            return [.. _items];
        }
    }

    public void Add(TaskItem task)
    {
        lock (_gate)
        {
            _items.Add(task);
        }
    }

    public bool Replace(TaskItem task)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(t => t.Id == task.Id);
            if (index < 0)
            {
                return false;
            }

            _items[index] = task;
            return true;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _items.Clear();
        }
    }
}
