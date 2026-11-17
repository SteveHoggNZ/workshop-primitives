namespace Lab;

/// <summary>Thin wrapper over the git command line.</summary>
internal static class Git
{
    public sealed record Change(string Code, string Path)
    {
        public bool Untracked => Code == "??";
    }

    public static Proc.Result Run(params string[] args) => Proc.Run("git", args);

    public static string Out(params string[] args)
    {
        var result = Run(args);
        if (result.ExitCode != 0)
        {
            throw new LabException($"git {string.Join(' ', args)} failed:\n{result.Output.Trim()}");
        }

        return result.Output;
    }

    public static IReadOnlyList<string> Subjects()
    {
        var result = Run("log", "--format=%s");
        return result.ExitCode == 0 ? Lines(result.Output) : [];
    }

    public static HashSet<string> FilesAtHead()
    {
        var result = Run("ls-tree", "-r", "--name-only", "HEAD");
        return result.ExitCode == 0 ? [.. Lines(result.Output)] : [];
    }

    /// <summary>Uncommitted changes, including untracked files (each listed individually).</summary>
    public static IReadOnlyList<Change> Status()
    {
        var changes = new List<Change>();
        foreach (var line in Out("status", "--porcelain=v1", "-uall").Split('\n'))
        {
            var text = line.TrimEnd('\r');
            if (text.Length < 4)
            {
                continue;
            }

            var path = text[3..];
            var arrow = path.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrow >= 0)
            {
                changes.Add(new Change("D ", Unquote(path[..arrow])));
                changes.Add(new Change("A ", Unquote(path[(arrow + 4)..])));
            }
            else
            {
                changes.Add(new Change(text[..2], Unquote(path)));
            }
        }

        return changes;
    }

    /// <summary>Commits exactly these paths, with a fallback identity if git has none configured.</summary>
    public static void Commit(string subject, IReadOnlyCollection<string> paths)
    {
        if (paths.Count > 0)
        {
            Out(["add", "--", .. paths]);
        }

        var identity = Run("config", "user.email").Output.Trim().Length > 0
            ? Array.Empty<string>()
            : ["-c", "user.name=Lab", "-c", "user.email=lab@example.invalid"];
        Out([.. identity, "commit", "-q", "--allow-empty", "-m", subject, "--", .. paths]);
    }

    private static List<string> Lines(string text) =>
        [.. text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0)];

    private static string Unquote(string path) =>
        path.Length >= 2 && path[0] == '"' && path[^1] == '"' ? path[1..^1] : path;
}
