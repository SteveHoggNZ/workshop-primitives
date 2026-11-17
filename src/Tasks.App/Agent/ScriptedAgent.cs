using Tasks.App.Services;

namespace Tasks.App.Agent;

/// <summary>One message and the agent's reply.</summary>
public sealed record AgentTurn(string Message, string Reply);

/// <summary>
/// An offline stand-in for a model: matches a message against <see cref="AgentIntents.All"/> and calls the tool.
/// Commands marked Confirm ask first and run only after "yes".
/// </summary>
public sealed class ScriptedAgent(TaskService tasks)
{
    private const int MaxTurns = 20;

    private readonly List<AgentTurn> _conversation = [];
    private readonly Lock _gate = new();
    private (Intent Intent, IReadOnlyDictionary<string, string> Args)? _pending;

    public IReadOnlyList<AgentTurn> Conversation
    {
        get
        {
            lock (_gate)
            {
                return [.. _conversation];
            }
        }
    }

    /// <summary>Reply to one message.</summary>
    public string Handle(string message)
    {
        lock (_gate)
        {
            var reply = Reply(message.Trim());
            _conversation.Add(new AgentTurn(message, reply));
            if (_conversation.Count > MaxTurns)
            {
                _conversation.RemoveAt(0);
            }

            return reply;
        }
    }

    /// <summary>The commands, generated from <see cref="AgentIntents.All"/>.</summary>
    public static string Help() =>
        "I understand:\n" + string.Join("\n", AgentIntents.All.Select(i => $"  {i.Usage}   (e.g. {i.Example})"));

    private string Reply(string text)
    {
        if (_pending is { } pending)
        {
            _pending = null;
            return IsYes(text) ? AgentTools.All[pending.Intent.Tool](tasks, pending.Args) : "Cancelled.";
        }

        if (text.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            return Help();
        }

        foreach (var intent in AgentIntents.All)
        {
            var match = intent.Pattern.Match(text);
            if (!match.Success)
            {
                continue;
            }

            IReadOnlyDictionary<string, string> args = match.Groups.Values
                .Where(g => g.Success && !char.IsAsciiDigit(g.Name[0]))
                .ToDictionary(g => g.Name, g => g.Value, StringComparer.Ordinal);
            if (intent.Confirm)
            {
                _pending = (intent, args);
                var what = intent.Preview is { } preview ? preview(tasks, args) : $"run '{intent.Usage}'";
                return $"This will {what}. Type yes to confirm.";
            }

            return AgentTools.All[intent.Tool](tasks, args);
        }

        return "Sorry, I don't understand that. Type help to see what I can do.";
    }

    private static bool IsYes(string text) =>
        text.Equals("yes", StringComparison.OrdinalIgnoreCase) || text.Equals("y", StringComparison.OrdinalIgnoreCase);
}
