namespace Lab;

/// <summary>Finds the repository root (the folder holding TasksLab.slnx).</summary>
internal static class Repo
{
    public const string Solution = "TasksLab.slnx";

    public static string Root { get; } = Find();

    public static string Rel(string path) =>
        Path.GetRelativePath(Root, path).Replace('\\', '/');

    private static string Find()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, Solution)))
                {
                    return dir.FullName;
                }
            }
        }

        throw new InvalidOperationException($"Can't find {Solution}. Run the lab from inside the repository.");
    }
}
