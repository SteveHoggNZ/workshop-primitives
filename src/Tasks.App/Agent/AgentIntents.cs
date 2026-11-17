using System.Text.RegularExpressions;

namespace Tasks.App.Agent;

/// <summary>The scripted agent's command table. Each row maps what the user types to a tool.</summary>
public static class AgentIntents
{
    public static readonly IReadOnlyList<Intent> All =
    [
        new("add", P(@"^add (?:task )?(?<title>.+)$"), "add_task", "add <title>", "add buy milk"),
        new("list", P(@"^(?:list|show)(?: tasks)?$"), "list_tasks", "list", "list"),
        new("complete", P(@"^(?:done|complete) (?<id>\S+)$"), "complete_task", "done <id>", "done seed3"),
        new("delete", P(@"^delete (?<id>\S+)$"), "delete_task", "delete <id>", "delete seed3", Confirm: true),
    ];

    private static Regex P(string pattern) => new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
