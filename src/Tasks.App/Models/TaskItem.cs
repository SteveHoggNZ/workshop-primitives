namespace Tasks.App.Models;

/// <summary>A task. Times are stored in UTC; pages convert them for display.</summary>
public sealed record TaskItem(string Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt = null)
{
    public bool Done => CompletedAt is not null;
}
