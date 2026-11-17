using System.Runtime.InteropServices;

namespace Lab;

/// <summary>Checks this machine can run the lab and says which steps need what.</summary>
internal static class Doctor
{
    private sealed record Check(string Name, string Status, string Detail, string NeededFor, bool Required);

    public static int Run()
    {
        var checks = new List<Check>
        {
            Sdk(),
            new("Runtime", "info", $"{RuntimeInformation.FrameworkDescription}, {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}", "", false),
            TimeZone(),
            Tool("git", ["--version"], "every step (reset, apply, goto)", required: true),
            Restore(),
            Tool("node", ["--version"], "optional browser track (Playwright MCP)", required: false),
            Tool("npx", ["--version"], "optional browser track (Playwright MCP)", required: false),
            Bicep(),
            Tool("copilot", ["--version"], "optional: Copilot CLI (VS Code agent mode works without it)", required: false),
        };

        Console.WriteLine($"Lab doctor  ({Repo.Root})");
        Console.WriteLine();
        foreach (var c in checks)
        {
            var mark = c.Status switch { "ok" => "OK  ", "info" => "    ", "missing" => "--  ", _ => "FAIL" };
            Console.WriteLine($"  {mark} {c.Name,-12} {c.Detail}");
            if (c.Status is "missing" or "fail" && c.NeededFor.Length > 0)
            {
                Console.WriteLine($"       {"",-12} needed for: {c.NeededFor}");
            }
        }

        var blocking = checks.Where(c => c.Required && c.Status is not ("ok" or "info")).ToList();
        Console.WriteLine();
        if (blocking.Count == 0)
        {
            Console.WriteLine("Ready: the core steps can run on this machine.");
            return 0;
        }

        Console.WriteLine($"Not ready: fix {string.Join(", ", blocking.Select(c => c.Name))} first.");
        return 1;
    }

    private static Check Sdk()
    {
        var result = Proc.Run("dotnet", ["--version"]);
        var text = result.Output.Trim();
        if (result.ExitCode != 0 || !Version.TryParse(text.Split('-')[0], out var version))
        {
            return new(".NET SDK", "fail", text.Length > 0 ? text : "dotnet not found", "every step", true);
        }

        return version.Major >= 10
            ? new(".NET SDK", "ok", text, "every step", true)
            : new(".NET SDK", "fail", $"{text}: the lab needs SDK 10.0.100 or later", "every step", true);
    }

    private static Check TimeZone()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
            var offset = zone.GetUtcOffset(DateTimeOffset.UtcNow);
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            return new("Time zone", "ok", $"Pacific/Auckland resolves (UTC{sign}{offset:hh\\:mm} now)", "steps that show times", true);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return new("Time zone", "fail", "Pacific/Auckland not found: is invariant globalization on, or ICU missing?", "steps that show times", true);
        }
    }

    private static Check Restore()
    {
        var result = Proc.Run("dotnet", ["restore", Repo.Solution, "-nologo", "-tl:off", "-v:q"], timeoutMs: 600_000);
        if (result.ExitCode == 0)
        {
            return new("NuGet", "ok", "packages restore", "every step", true);
        }

        var firstError = result.Output.Split('\n').FirstOrDefault(l => l.Contains("error", StringComparison.OrdinalIgnoreCase))?.Trim()
            ?? result.Output.Trim();
        return new("NuGet", "fail", $"restore failed: {firstError}", "every step (check your NuGet feed or proxy)", true);
    }

    private static Check Bicep()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var azureCliBicep = Path.Combine(home, ".azure", "bin", OperatingSystem.IsWindows() ? "bicep.exe" : "bicep");
        var path = Proc.Find("bicep") ?? (File.Exists(azureCliBicep) ? azureCliBicep : null);
        if (path is null)
        {
            return new("Bicep CLI", "missing", "not installed (later steps will get Bicep from NuGet instead)", "later steps (Bicep lint)", false);
        }

        var result = Proc.Run(path, ["--version"]);
        return new("Bicep CLI", result.ExitCode == 0 ? "ok" : "fail", FirstLine(result.Output), "later steps (Bicep lint)", false);
    }

    private static Check Tool(string name, string[] args, string neededFor, bool required)
    {
        var path = Proc.Find(name);
        if (path is null)
        {
            return new(name, required ? "fail" : "missing", "not found on PATH", neededFor, required);
        }

        var result = Proc.Run(path, args);
        return new(name, result.ExitCode == 0 ? "ok" : "fail", FirstLine(result.Output), neededFor, required);
    }

    private static string FirstLine(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
}
