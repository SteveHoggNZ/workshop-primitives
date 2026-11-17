using Tasks.App.Models;

namespace Tasks.App.Services;

/// <summary>Read-only task queries, for code that only needs to look.</summary>
public interface ITaskQueries
{
    IReadOnlyList<TaskItem> Open();

    IReadOnlyList<TaskItem> Recent(int count = 5);
}
