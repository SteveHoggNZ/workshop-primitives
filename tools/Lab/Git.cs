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

    /// <summary>The checked-out branch, or null when HEAD is detached.</summary>
    public static string? CurrentBranch()
    {
        var result = Run("symbolic-ref", "--quiet", "--short", "HEAD");
        return result.ExitCode == 0 && result.Output.Trim().Length > 0 ? result.Output.Trim() : null;
    }

    /// <summary>
    /// Lab commands that commit (apply, goto, scenario, ratchet) refuse to run on main or master, so step commits
    /// never land in the lab's own history. LAB_ALLOW_MAIN=1 overrides it (the validation harness's clones use main).
    /// </summary>
    public static void RefuseOnMain(string command)
    {
        if (Environment.GetEnvironmentVariable("LAB_ALLOW_MAIN") == "1")
        {
            return;
        }

        var branch = CurrentBranch();
        if (branch is "main" or "master")
        {
            throw new LabException(
                $"./lab {command} commits to the current branch, and you're on {branch}. Do the lab on your own branch, " +
                "so main stays clean:\n\n" +
                "  git switch -c lab/<your-name>     (or, if you already have one: git switch lab/<your-name>)\n\n" +
                "Then run the command again. Nothing was changed.");
        }
    }

    /// <summary>Commits exactly these paths, with a fallback identity if git has none configured.</summary>
    public static void Commit(string subject, IReadOnlyCollection<string> paths)
    {
        RefuseOnMain("commit");
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
