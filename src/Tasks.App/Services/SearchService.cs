namespace Tasks.App.Services;

/// <summary>Search over task titles, ignoring case and stop words.</summary>
public sealed class SearchService(TaskService tasks)
{
    public int Count(string query)
    {
        var wanted = TextHelpers.Normalise(query);
        return tasks.List().Count(t => TextHelpers.Normalise(t.Title).Contains(wanted, StringComparison.Ordinal));
    }
}
