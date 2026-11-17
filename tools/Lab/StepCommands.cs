namespace Lab;

/// <summary>list, apply, reset and goto: moving the repo between Before and After states.</summary>
internal static class StepCommands
{
    public static int List()
    {
        var subjects = Git.Subjects();
        var steps = Steps.All();
        Console.WriteLine("Steps (x = installed):");
        foreach (var step in steps)
        {
            var mark = Steps.Installed(step, subjects) ? "x" : " ";
            var solution = subjects.Contains(step.SolutionSubject) ? "  + solution" : "";
            Console.WriteLine($"  [{mark}] {step.Number:00}  {step.Name}{solution}");
        }

        var next = steps.FirstOrDefault(s => !Steps.Installed(s, subjects));
        Console.WriteLine();
        Console.WriteLine(next is null
            ? "All steps are installed."
            : $"Next: step {next.Number}. Run the Before first, then ./lab reset and ./lab apply {next.Number}.");
        return 0;
    }

    public static int Apply(string[] args)
    {
        var (number, tools, _) = Parse(args, "apply");
        var step = Steps.Get(number);
        if (Steps.Installed(step, Git.Subjects()))
        {
            Console.WriteLine($"Step {step.Number} ({step.Name}) is already installed.");
            return 0;
        }

        RequireClean(ExampleFiles(step, tools).Select(f => f.Target).ToHashSet(StringComparer.Ordinal));
        var installed = Install(step, tools);
        Console.WriteLine($"Installed step {step.Number} ({step.Name}): {installed.Count} file(s), committed as '{step.Subject}'.");
        foreach (var file in installed)
        {
            Console.WriteLine($"  {file}");
        }

        Console.WriteLine();
        Console.WriteLine("Next: read the installed files, then start a NEW agent session for the After run.");
        return 0;
    }

    public static int Goto(string[] args)
    {
        var (number, tools, solutions) = Parse(args, "goto");
        var steps = Steps.All();
        if (number < 0 || number > steps.Max(s => s.Number) + 1)
        {
            throw new LabException($"goto {number}: steps run from 0 to {steps.Max(s => s.Number)}.");
        }

        RequireClean([]);
        var subjects = Git.Subjects();
        var did = 0;
        foreach (var step in steps.Where(s => s.Number < number))
        {
            if (!Steps.Installed(step, subjects))
            {
                Install(step, tools);
                Console.WriteLine($"  installed step {step.Number} ({step.Name})");
                did++;
            }

            if (solutions && File.Exists(step.SolutionPatch) && !subjects.Contains(step.SolutionSubject))
            {
                Git.Out("apply", "--index", step.SolutionPatch);
                var changed = Git.Status().Where(c => !c.Untracked).Select(c => c.Path).ToList();
                Git.Commit(step.SolutionSubject, changed);
                Console.WriteLine($"  applied the reference solution for step {step.Number}");
                did++;
            }
        }

        Console.WriteLine(did == 0
            ? $"Nothing to do: everything before step {number} is already installed."
            : $"Ready for step {number}.");
        return 0;
    }

    public static int Reset(string[] args)
    {
        var dryRun = args.Contains("--dry-run");
        var changes = Git.Status();
        var managed = changes.Where(c => Steps.IsManaged(c.Path)).ToList();
        var leftAlone = changes.Where(c => !Steps.IsManaged(c.Path)).Select(c => c.Path).Distinct().ToList();
        var atHead = Git.FilesAtHead();

        var restore = managed.Where(c => !c.Untracked && atHead.Contains(c.Path)).Select(c => c.Path).Distinct().ToList();
        var remove = managed.Where(c => c.Untracked || !atHead.Contains(c.Path)).Select(c => c.Path).Distinct().ToList();

        if (restore.Count == 0 && remove.Count == 0)
        {
            Console.WriteLine("Nothing to reset: the lab's folders match the last commit.");
        }
        else
        {
            var verb = dryRun ? "Would reset" : "Reset";
            Console.WriteLine($"{verb}: {restore.Count} changed file(s) restored, {remove.Count} new file(s) removed.");
            foreach (var path in restore)
            {
                Console.WriteLine($"  restore  {path}");
            }

            foreach (var path in remove)
            {
                Console.WriteLine($"  remove   {path}");
            }

            if (!dryRun)
            {
                if (restore.Count > 0)
                {
                    Git.Out(["restore", "--source=HEAD", "--staged", "--worktree", "--", .. restore]);
                }

                foreach (var path in remove)
                {
                    Git.Run("rm", "-q", "--cached", "--ignore-unmatch", "--", path);
                    var full = Path.Combine(Repo.Root, path);
                    if (File.Exists(full))
                    {
                        File.Delete(full);
                    }

                    RemoveEmptyParents(Path.GetDirectoryName(full));
                }
            }
        }

        if (leftAlone.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Left alone (outside the lab's folders):");
            foreach (var path in leftAlone)
            {
                Console.WriteLine($"  {path}");
            }
        }

        return 0;
    }

    private sealed record ExampleFile(string Source, string Target);

    private static List<ExampleFile> ExampleFiles(Step step, string[] tools)
    {
        var files = new List<ExampleFile>();
        AddTree(files, step.Dir, skip: "_other-tools");
        foreach (var tool in tools)
        {
            var adapter = Path.Combine(step.Dir, "_other-tools", tool);
            if (Directory.Exists(adapter))
            {
                AddTree(files, adapter, skip: null);
            }
        }

        return files;
    }

    private static void AddTree(List<ExampleFile> files, string root, string? skip)
    {
        foreach (var source in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(root, source).Replace('\\', '/');
            if (skip is not null && (rel == skip || rel.StartsWith(skip + "/", StringComparison.Ordinal)))
            {
                continue;
            }

            if (Path.GetFileName(rel) == ".DS_Store")
            {
                continue;
            }

            files.Add(new ExampleFile(source, rel));
        }
    }

    private static List<string> Install(Step step, string[] tools)
    {
        var files = ExampleFiles(step, tools);
        foreach (var file in files)
        {
            var target = Path.Combine(Repo.Root, file.Target);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file.Source, target, overwrite: true);
        }

        var targets = files.Select(f => f.Target).ToList();
        Git.Commit(step.Subject, targets);
        return targets;
    }

    /// <summary>
    /// Refuse to install on top of uncommitted work, so a Before run never ends up inside a step's commit.
    /// Untracked files outside the lab's folders are fine; so are files identical to the ones being installed.
    /// </summary>
    private static void RequireClean(HashSet<string> installing)
    {
        var blocking = Git.Status()
            .Where(c => !c.Untracked || Steps.IsManaged(c.Path))
            .Where(c => !(c.Untracked && installing.Contains(c.Path)))
            .Select(c => c.Path)
            .Distinct()
            .ToList();
        if (blocking.Count == 0)
        {
            return;
        }

        var sample = string.Join("\n", blocking.Take(8).Select(p => "  " + p));
        var more = blocking.Count > 8 ? $"\n  ... and {blocking.Count - 8} more" : "";
        throw new LabException(
            $"There are uncommitted changes:\n{sample}{more}\n\n" +
            "If they're from a Before run, ./lab reset discards them. If you want to keep them, commit them first.");
    }

    private static (int Number, string[] Tools, bool Solutions) Parse(string[] args, string command)
    {
        var number = args.FirstOrDefault(a => !a.StartsWith('-'));
        if (number is null || !int.TryParse(number, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n))
        {
            throw new LabException($"Usage: ./lab {command} <step number>   (./lab list shows them)");
        }

        var toolsIndex = Array.IndexOf(args, "--tools");
        var tools = toolsIndex >= 0 && toolsIndex + 1 < args.Length
            ? args[toolsIndex + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
        return (n, tools, args.Contains("--solutions"));
    }

    private static void RemoveEmptyParents(string? dir)
    {
        while (dir is not null
               && dir.Length > Repo.Root.Length
               && Directory.Exists(dir)
               && !Directory.EnumerateFileSystemEntries(dir).Any())
        {
            Directory.Delete(dir);
            dir = Path.GetDirectoryName(dir);
        }
    }
}
