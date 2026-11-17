using System.Text.Json;
using System.Xml.Linq;

namespace Lab;

/// <summary>
/// Test-first phases for the tdd skill, recorded in .tdd-phase: red (write a failing test), green (make it pass),
/// done. `green` only starts if a test really fails (an assertion or the bug's exception), not if the build fails:
/// a test that doesn't compile proves nothing about the behaviour. `done` only finishes if the whole suite passes,
/// including the tests that failed in red.
/// </summary>
internal static class Tdd
{
    private static readonly string PhaseFile = Path.Combine(Repo.Root, ".tdd-phase");
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private sealed record State(string Phase, string Behaviour, List<string> Failing);

    private sealed record TestRun(bool Built, int Passed, List<(string Name, string Message)> Failed, string BuildErrors, HashSet<string> Names);

    public static int Run(string[] args)
    {
        var cmd = args.Length > 0 ? args[0] : "status";
        var state = Load();
        switch (cmd)
        {
            case "status":
                Console.WriteLine(state is null ? "Phase: none." : $"Phase: {state.Phase}. Behaviour: {state.Behaviour}");
                return 0;
            case "red":
                var behaviour = string.Join(' ', args.Skip(1)).Trim();
                if (behaviour.Length == 0)
                {
                    Console.WriteLine("Say which behaviour: ./lab tdd red \"empty dashboard shows 0%\"");
                    return 2;
                }

                Save(new State("red", behaviour, []));
                Console.WriteLine($"RED: write one failing test under tests/ for: {behaviour}. Don't edit src/ yet.");
                Console.WriteLine("Then run ./lab tdd green.");
                return 0;
            case "green":
                return Green(state);
            case "done":
                return Done(state);
            default:
                Console.WriteLine("Usage: ./lab tdd red \"<behaviour>\" | green | done | status");
                return 2;
        }
    }

    private static int Green(State? state)
    {
        if (state?.Phase != "red")
        {
            Console.WriteLine($"Refused: phase is {state?.Phase ?? "none"}. Start with ./lab tdd red \"<behaviour>\".");
            return 1;
        }

        var run = Test();
        if (!run.Built)
        {
            Console.WriteLine("Refused: the tests don't build, so nothing failed for the right reason. That isn't red.");
            Console.WriteLine("Fix the test so it compiles against the code as it is, and fails on an assertion.");
            Console.WriteLine(run.BuildErrors);
            return 1;
        }

        if (run.Failed.Count == 0)
        {
            Console.WriteLine($"Refused: no test fails ({run.Passed} passed). Red means a new test fails because of the missing behaviour.");
            return 1;
        }

        Save(state with { Phase = "green", Failing = [.. run.Failed.Select(f => f.Name)] });
        Console.WriteLine($"GREEN: {run.Failed.Count} failing, for the right reason:");
        foreach (var (name, message) in run.Failed)
        {
            Console.WriteLine($"  FAIL {name}: {message}");
        }

        Console.WriteLine("Now make the smallest change in src/ that makes it pass. Don't edit the failing test. Then ./lab tdd done.");
        return 0;
    }

    private static int Done(State? state)
    {
        if (state?.Phase != "green")
        {
            Console.WriteLine($"Refused: phase is {state?.Phase ?? "none"}. Run ./lab tdd green first.");
            return 1;
        }

        var run = Test();
        if (!run.Built || run.Failed.Count > 0)
        {
            Console.WriteLine(run.Built ? "Refused: the suite still fails:" : "Refused: the tests don't build:");
            foreach (var (name, message) in run.Failed)
            {
                Console.WriteLine($"  FAIL {name}: {message}");
            }

            Console.WriteLine(run.BuildErrors);
            return 1;
        }

        var gone = state.Failing.Where(f => !run.Names.Contains(f)).ToList();
        if (gone.Count > 0)
        {
            Console.WriteLine($"Refused: the test that failed in red is gone: {string.Join(", ", gone)}. Keep it; make it pass.");
            return 1;
        }

        File.Delete(PhaseFile);
        Console.WriteLine($"DONE: {state.Behaviour}. Now passing: {string.Join(", ", state.Failing)} ({run.Passed} passed in all).");
        return 0;
    }

    private static TestRun Test()
    {
        var results = Path.Combine(Path.GetTempPath(), "lab-tdd-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            var r = Proc.Run("dotnet", ["test", "-nologo", "-tl:off", "--logger", "trx;LogFileName=t.trx", "--results-directory", results],
                timeoutMs: 300_000);
            var trx = Directory.Exists(results) ? Directory.GetFiles(results, "*.trx", SearchOption.AllDirectories) : [];
            if (trx.Length == 0)
            {
                var errors = r.Output.Split('\n').Where(l => l.Contains(": error ", StringComparison.Ordinal)).Select(l => "  " + l.Trim()).Distinct().Take(10);
                return new TestRun(false, 0, [], string.Join(Environment.NewLine, errors), []);
            }

            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var failed = new List<(string, string)>();
            var passed = 0;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in trx)
            {
                foreach (var res in XDocument.Load(file).Descendants(ns + "UnitTestResult"))
                {
                    var name = (string?)res.Attribute("testName") ?? "";
                    names.Add(name);
                    var outcome = (string?)res.Attribute("outcome");
                    if (outcome == "Passed")
                    {
                        passed++;
                    }
                    else if (outcome == "Failed")
                    {
                        var message = res.Descendants(ns + "Message").FirstOrDefault()?.Value ?? "";
                        failed.Add((name, string.Join(' ', message.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(3))));
                    }
                }
            }

            return new TestRun(true, passed, failed, "", names);
        }
        finally
        {
            if (Directory.Exists(results))
            {
                Directory.Delete(results, recursive: true);
            }
        }
    }

    private static State? Load()
    {
        if (!File.Exists(PhaseFile))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<State>(File.ReadAllText(PhaseFile));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Save(State state) => File.WriteAllText(PhaseFile, JsonSerializer.Serialize(state, Indented) + Environment.NewLine);
}
