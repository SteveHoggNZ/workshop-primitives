using Tasks.App.Services;

namespace Tasks.App.Agent;

/// <summary>A tool the agent can call: it gets the task service and the command's arguments, and returns the reply.</summary>
public delegate string AgentTool(TaskService tasks, IReadOnlyDictionary<string, string> args);

/// <summary>The tools the in-app agent can call.</summary>
public static class AgentTools
{
    public static readonly IReadOnlyDictionary<string, AgentTool> All = new Dictionary<string, AgentTool>(StringComparer.Ordinal)
    {
        ["add_task"] = AddTask,
        ["list_tasks"] = ListTasks,
        ["complete_task"] = CompleteTask,
        ["delete_task"] = DeleteTask,
    };

    /// <summary>Add a task with the given title.</summary>
    public static string AddTask(TaskService tasks, IReadOnlyDictionary<string, string> args)
    {
        var task = tasks.Add(args["title"]);
        return $"Added task {task.Id}: {task.Title}";
    }

    /// <summary>List all tasks.</summary>
    public static string ListTasks(TaskService tasks, IReadOnlyDictionary<string, string> args)
    {
        var all = tasks.List();
        return all.Count == 0
            ? "No tasks."
            : string.Join("\n", all.Select(t => $"{t.Id}  [{(t.Done ? "x" : " ")}] {t.Title}"));
    }

    /// <summary>Mark one task done.</summary>
    public static string CompleteTask(TaskService tasks, IReadOnlyDictionary<string, string> args)
    {
        var id = args["id"];
        return tasks.Complete(id) is { } task ? $"Done: {task.Title}" : $"There is no task {id}.";
    }

    /// <summary>Delete one task.</summary>
    public static string DeleteTask(TaskService tasks, IReadOnlyDictionary<string, string> args)
    {
        var id = args["id"];
        return tasks.Delete(id) ? $"Deleted task {id}." : $"There is no task {id}.";
    }
}
