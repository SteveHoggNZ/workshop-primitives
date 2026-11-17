using System.Diagnostics;

namespace Lab;

/// <summary>
/// One-off setup cost, paid before the day instead of during a step: restore and build the solution, and build
/// each skill script (file-based apps under .lab/examples) once, so their NuGet packages are in the cache. Run it
/// after ./lab doctor. It changes nothing in the repo except the gitignored .lab/.warm folder.
/// </summary>
internal static class Warm
{
    public static int Run()
    {
        var total = Stopwatch.StartNew();
        var ok = Step("restore and build the app and tests", "dotnet", ["build", "TasksLab.slnx", "-nologo", "-tl:off", "-v:q"]);

        var examples = Path.Combine(Repo.Root, ".lab", "examples");
        IEnumerable<string> scripts = Directory.Exists(examples)
            ? Directory.GetFiles(examples, "*.cs", SearchOption.AllDirectories)
                .Where(f => f.Contains(Path.Combine(".agents", "skills"), StringComparison.Ordinal) && Path.GetFileName(Path.GetDirectoryName(f)) == "scripts")
                .Order(StringComparer.Ordinal)
            : [];
        foreach (var script in scripts)
        {
            // A copy four folders below the root, so its "#:project ../../../../src/..." line resolves as it does
            // from .agents/skills/<skill>/scripts/.
            var dir = Path.Combine(Repo.Root, ".lab", ".warm", "skills", Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(script))!));
            Directory.CreateDirectory(dir);
            var copy = Path.Combine(dir, Path.GetFileName(script));
            File.Copy(script, copy, overwrite: true);
            // Run with no arguments: the build (and package restore) is the point; the scripts print usage or nothing.
            ok &= Step($"build skill script {Path.GetRelativePath(Repo.Root, script)}", "dotnet", ["run", "--no-cache", copy], okCodes: [0, 2]);
        }

        Console.WriteLine(ok
            ? $"Warm: done in {total.Elapsed.TotalSeconds:F0} s. The steps' commands now start without downloads."
            : "Warm: something failed (above). Run ./lab doctor, fix what it reports, then ./lab warm again.");
        return ok ? 0 : 1;
    }

    private static bool Step(string what, string file, string[] args, int[]? okCodes = null)
    {
        okCodes ??= [0];
        var sw = Stopwatch.StartNew();
        var r = Proc.Run(file, args, timeoutMs: 900_000);
        var ok = okCodes.Contains(r.ExitCode);
        Console.WriteLine($"  {(ok ? "ok    " : "FAILED")}  {what}  ({sw.Elapsed.TotalSeconds:F0} s)");
        if (!ok)
        {
            foreach (var line in r.Output.Split('\n').Where(l => l.Contains("error", StringComparison.OrdinalIgnoreCase)).Take(8))
            {
                Console.WriteLine("          " + line.Trim());
            }
        }

        return ok;
    }
}
