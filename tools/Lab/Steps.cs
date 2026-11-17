using System.Text.RegularExpressions;

namespace Lab;

/// <summary>A lab step: one example folder under .lab/examples, installed as its own commit.</summary>
internal sealed record Step(int Number, string Name, string Dir)
{
    public string Id => $"step{Number:00}";

    public string Subject => $"lab: {Id} {Name}";

    public string SolutionSubject => $"lab: {Id} solution";

    public string SolutionPatch => Path.Combine(Repo.Root, ".lab", "solutions", $"{Id}.patch");
}

internal static partial class Steps
{
    /// <summary>
    /// The folders a Before/After run can change, and the only ones `reset` touches.
    /// Anything else (notes, .vscode, .lab) is left alone.
    /// </summary>
    public static readonly string[] Managed =
    [
        "src/", "tests/", "docs/", "infra/", "specs/",
        ".github/", ".agents/", ".claude/", ".codex/", ".specify/",
        "AGENTS.md", "CLAUDE.md", "CHANGELOG.md", "README.md", ".mcp.json", ".tdd-phase",
    ];

    public static bool IsManaged(string path) =>
        Managed.Any(m => m.EndsWith('/') ? path.StartsWith(m, StringComparison.Ordinal) : path == m);

    public static IReadOnlyList<Step> All()
    {
        var examples = Path.Combine(Repo.Root, ".lab", "examples");
        if (!Directory.Exists(examples))
        {
            throw new LabException("No .lab/examples folder: this copy of the repo doesn't have the lab material.");
        }

        var steps = new List<Step>();
        foreach (var dir in Directory.GetDirectories(examples))
        {
            var match = StepFolder().Match(Path.GetFileName(dir));
            if (match.Success)
            {
                steps.Add(new Step(int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), match.Groups[2].Value, dir));
            }
        }

        return [.. steps.OrderBy(s => s.Number)];
    }

    public static Step Get(int number) =>
        All().FirstOrDefault(s => s.Number == number)
        ?? throw new LabException($"There's no step {number}. Run ./lab list to see the steps.");

    public static bool Installed(Step step, IReadOnlyList<string> subjects) => subjects.Contains(step.Subject);

    [GeneratedRegex(@"^step(\d\d)-(.+)$")]
    private static partial Regex StepFolder();
}
