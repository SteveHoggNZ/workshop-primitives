using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lab;

/// <summary>
/// Builds the app from scratch and counts its warnings: the compiler's nullable warnings plus the
/// analyzer rules turned on in src/.editorconfig. The VS Code Problems panel shows the same set.
/// </summary>
internal static partial class Lint
{
    public sealed record Finding(string Rule, string File, int Line, int Column, string Message);

    private static readonly string Project = Path.Combine("src", "Tasks.App", "Tasks.App.csproj");
    private static readonly string BaselineFile = Path.Combine("docs", "lint-baseline.md");

    public static int Run(string[] args)
    {
        var json = args.Contains("--json");
        var check = args.Contains("--check");

        var build = Proc.Run(
            "dotnet",
            ["build", Project, "--no-incremental", "-nologo", "-tl:off", "-v:m", "-clp:NoSummary",
             "-p:TreatWarningsAsErrors=false"],
            timeoutMs: 600_000);

        if (build.ExitCode != 0)
        {
            Console.Error.WriteLine("The build failed, so nothing was counted. Errors:");
            foreach (var line in build.Output.Split('\n').Where(l => l.Contains(": error ", StringComparison.Ordinal)).Distinct())
            {
                Console.Error.WriteLine("  " + line.Trim());
            }

            return 2;
        }

        var findings = Parse(build.Output);
        var baseline = ReadBaseline();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                total = findings.Count,
                baseline,
                byRule = findings.GroupBy(f => f.Rule).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => g.Count()),
                findings,
            }));
        }
        else
        {
            foreach (var f in findings)
            {
                Console.WriteLine($"  {f.File}:{f.Line}  {f.Rule}  {f.Message}");
            }

            if (findings.Count > 0)
            {
                Console.WriteLine();
            }

            Console.WriteLine(baseline is null ? $"Total: {findings.Count}" : $"Total: {findings.Count} (baseline {baseline})");
        }

        if (check && baseline is not null && findings.Count > baseline)
        {
            Console.Error.WriteLine($"Lint check failed: {findings.Count} findings, baseline is {baseline}.");
            return 1;
        }

        return 0;
    }

    public static List<Finding> Parse(string buildOutput)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var findings = new List<Finding>();
        foreach (var raw in buildOutput.Split('\n'))
        {
            var match = WarningLine().Match(raw.TrimEnd('\r'));
            if (!match.Success)
            {
                continue;
            }

            var finding = new Finding(
                match.Groups["rule"].Value,
                Repo.Rel(match.Groups["file"].Value.Trim()),
                int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(match.Groups["col"].Value, System.Globalization.CultureInfo.InvariantCulture),
                match.Groups["msg"].Value.Trim());
            if (seen.Add($"{finding.Rule}|{finding.File}|{finding.Line}|{finding.Column}|{finding.Message}"))
            {
                findings.Add(finding);
            }
        }

        return [.. findings.OrderBy(f => f.File, StringComparer.Ordinal).ThenBy(f => f.Line).ThenBy(f => f.Rule, StringComparer.Ordinal)];
    }

    private static int? ReadBaseline()
    {
        var path = Path.Combine(Repo.Root, BaselineFile);
        if (!File.Exists(path))
        {
            return null;
        }

        var match = BaselineLine().Match(File.ReadAllText(path));
        return match.Success ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null;
    }

    [GeneratedRegex(@"^\s*(?<file>.+?)\((?<line>\d+),(?<col>\d+)\): warning (?<rule>[A-Za-z]+\d+): (?<msg>.*?)(?:\s+\[[^\]]+\])?$")]
    private static partial Regex WarningLine();

    [GeneratedRegex(@"^Total:\s*(\d+)", RegexOptions.Multiline)]
    private static partial Regex BaselineLine();
}
