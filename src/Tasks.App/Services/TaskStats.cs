namespace Tasks.App.Services;

/// <summary>Numbers for the dashboard.</summary>
public static class TaskStats
{
    /// <summary>The share of tasks that are done, as a whole percentage.</summary>
    public static int CompletionPercent(int done, int total) => done * 100 / total;

    /// <summary>The Monday the current week started on, for a user in <paramref name="zone"/>.</summary>
    public static DateOnly WeekStart(DateTimeOffset now, TimeZoneInfo zone)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime.Date);
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysSinceMonday);
    }
}
