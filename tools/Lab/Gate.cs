using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Lab;

/// <summary>
/// The quality gate, run as an agentStop hook (`./lab hook gate`): when the agent says it's finished, check the bar
/// and send it back if the work doesn't meet it. The agent's own report isn't the gate; this is.
///   - lint at or below docs/lint-baseline.md (./lab lint --check)
///   - no new suppressions (#pragma warning disable, [SuppressMessage], NoWarn, a rule turned off in .editorconfig)
///   - the bar itself untouched (the baseline file, .editorconfig files, Directory.Build.props)
///   - dotnet test passes
///   - dotnet format whitespace --verify-no-changes (the whole solution)
/// Lint and tests run in parallel. Nothing changed under src/ or tests/: it passes at once.
/// Output: nothing to let the agent stop, or {"decision":"block","reason":...} to send it back.
/// </summary>
internal static partial class Gate
{
    public static readonly string[] Bar = ["docs/lint-baseline.md", "src/.editorconfig", ".editorconfig", "Directory.Build.props"];

    public static int Run(string[] args)
    {
        _ = Console.In.ReadToEnd(); // the agentStop event; nothing in it changes the checks
        var changes = Git.Status().Select(c => c.Path).ToList();
        if (!changes.Any(p => p.StartsWith("src/", StringComparison.Ordinal) || p.StartsWith("tests/", StringComparison.Ordinal) || Bar.Contains(p)))
        {
            return 0;
        }

        var sw = Stopwatch.StartNew();
        var problems = new List<string>();
        var dll = typeof(Gate).Assembly.Location;
        var lintTask = Task.Run(() => Proc.Run("dotnet", [dll, "lint", "--json"], timeoutMs: 150_000));
        var testTask = Task.Run(() => Proc.Run("dotnet", ["test", "-nologo", "-tl:off", "-v:q"], timeoutMs: 150_000));
        var formatTask = Task.Run(() => Proc.Run("dotnet", ["format", "whitespace", "TasksLab.slnx", "--verify-no-changes", "-v", "q"], timeoutMs: 150_000));

        var moved = changes.Where(p => Bar.Contains(p)).ToList();
        if (moved.Count > 0)
        {
            problems.Add($"The lint bar changed ({string.Join(", ", moved)}). People move the bar, not a check: put it back and fix the findings.");
        }

        var suppressions = AddedLines().Where(l => Suppression().IsMatch(l)).Take(5).ToList();
        if (suppressions.Count > 0)
        {
            problems.Add("New suppressions hide findings instead of fixing them: " + string.Join(" | ", suppressions.Select(s => s.Trim())));
        }

        var lint = lintTask.Result;
        try
        {
            var json = JsonNode.Parse(lint.Output.Trim().Split('\n')[^1]);
            var total = (int?)json?["total"];
            var baseline = (int?)json?["baseline"];
            if (total is null)
            {
                problems.Add("./lab lint failed (the build has errors): fix them.");
            }
            else if (baseline is not null && total > baseline)
            {
                var findings = json?["findings"]?.AsArray().Select(f => $"{f?["File"]}:{f?["Line"]} {f?["Rule"]}").Take(8) ?? [];
                problems.Add($"Lint: Total {total} (baseline {baseline}). Bring it down to {baseline} or below by fixing findings, " +
                             $"old ones included (they count the same): {string.Join(", ", findings)}.");
            }
        }
        catch (JsonException)
        {
            problems.Add("./lab lint failed: " + lint.Output.Trim().Split('\n').LastOrDefault());
        }

        var test = testTask.Result;
        if (test.ExitCode != 0)
        {
            var failed = test.Output.Split('\n').Where(l => l.Contains("Failed ", StringComparison.Ordinal) || l.Contains(": error ", StringComparison.Ordinal)).Take(5);
            problems.Add("dotnet test fails: " + string.Join(" | ", failed.Select(l => l.Trim())));
        }

        var format = formatTask.Result;
        if (format.ExitCode != 0)
        {
            var files = FormatFile().Matches(format.Output).Select(m => Repo.Rel(m.Groups[1].Value)).Distinct().Take(8);
            problems.Add("Format: not formatted (old files count too): " + string.Join(", ", files) +
                         ". Run `dotnet format whitespace TasksLab.slnx` and check the diff.");
        }

        if (problems.Count == 0)
        {
            return 0;
        }

        var reason = "Quality gate failed. You're not done yet: fix these, then finish. " +
                     $"(Checked in {sw.Elapsed.TotalSeconds:F0} s.)\n- " + string.Join("\n- ", problems);
        Console.WriteLine(JsonSerializer.Serialize(new JsonObject { ["decision"] = "block", ["reason"] = reason }));
        return 0;
    }

    /// <summary>Lines added since HEAD, in changed and new files.</summary>
    private static IEnumerable<string> AddedLines()
    {
        foreach (var line in Git.Run(["diff", "HEAD", "--unified=0", "--", "src/", "tests/", .. Bar]).Output.Split('\n'))
        {
            if (line.StartsWith('+') && !line.StartsWith("+++", StringComparison.Ordinal))
            {
                yield return line[1..];
            }
        }

        foreach (var c in Git.Status().Where(c => c.Untracked && (c.Path.StartsWith("src/", StringComparison.Ordinal) || c.Path.StartsWith("tests/", StringComparison.Ordinal))))
        {
            var path = Path.Combine(Repo.Root, c.Path);
            if (File.Exists(path) && (path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".csproj", StringComparison.Ordinal) || path.EndsWith(".props", StringComparison.Ordinal) || path.EndsWith(".editorconfig", StringComparison.Ordinal)))
            {
                foreach (var l in File.ReadAllLines(path))
                {
                    yield return l;
                }
            }
        }
    }

    [GeneratedRegex(@"^\s*(.+?\.cs)\(\d+,\d+\): error WHITESPACE", RegexOptions.Multiline)]
    private static partial Regex FormatFile();

    [GeneratedRegex(@"#pragma\s+warning\s+disable|SuppressMessage|<NoWarn>|dotnet_diagnostic\.\w+\.severity\s*=\s*(none|silent|suggestion)|#nullable\s+disable", RegexOptions.IgnoreCase)]
    private static partial Regex Suppression();
}
