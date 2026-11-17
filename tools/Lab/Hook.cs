using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Lab;

/// <summary>
/// Agent hooks, run by the agent tool (Copilot CLI and VS Code read .github/hooks/*.json). One prebuilt command
/// for every shell, so the same hook runs on macOS, Linux and Windows.
///
///   hook guard   preToolUse: changes under infra/prod/ need a human ("ask"). Reads the tool call as JSON on stdin,
///                prints {"permissionDecision": ..., "permissionDecisionReason": ...}. File tools are checked by
///                path; shell commands by pattern (sed -i, redirects, tee, mv/cp, Set-Content, ...), which is the
///                limit: a command that writes without naming the file (cd infra/prod && terraform fmt) isn't seen.
///                --sleep N waits N seconds first (to see what a hook timeout does).
/// </summary>
internal static partial class Hook
{
    private const string Protected = "infra/prod/";

    public static int Run(string[] args)
    {
        var sub = args.Length > 0 ? args[0] : "";
        if (sub != "guard")
        {
            Console.Error.WriteLine("Usage: ./lab hook guard [--sleep N]   (reads a preToolUse event as JSON on stdin)");
            return 2;
        }

        var sleep = Array.IndexOf(args, "--sleep");
        if (sleep >= 0 && sleep + 1 < args.Length && int.TryParse(args[sleep + 1], out var seconds))
        {
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }

        var input = Console.In.ReadToEnd();
        JsonObject? ev;
        try
        {
            ev = JsonNode.Parse(input) as JsonObject;
        }
        catch (JsonException)
        {
            ev = null;
        }

        if (ev is null)
        {
            return 0; // not a tool call we understand: no opinion
        }

        var tool = (string?)(ev["toolName"] ?? ev["tool_name"]) ?? "";
        var cwd = (string?)ev["cwd"] ?? Directory.GetCurrentDirectory();
        var toolArgs = ev["toolArgs"] ?? ev["tool_input"];
        if (toolArgs is JsonValue v && v.TryGetValue<string>(out var text))
        {
            try
            {
                toolArgs = JsonNode.Parse(text);
            }
            catch (JsonException)
            {
                toolArgs = JsonValue.Create(text);
            }
        }

        var hit = Touches(tool, toolArgs, cwd);
        if (hit is null)
        {
            return 0;
        }

        Reply("ask",
            $"{hit} is under infra/prod/: production infrastructure. Changes there need a person from the platform team to approve " +
            "them. Don't try another way to make the change. Stop, tell the user it needs their approval, and show the exact " +
            "change you'd make (the file and the new lines) so they can apply it.");
        return 0;
    }

    /// <summary>The protected path this call would change, or null.</summary>
    private static string? Touches(string tool, JsonNode? args, string cwd)
    {
        var shell = tool is "bash" or "shell" or "powershell" or "Bash" or "PowerShell";
        if (shell)
        {
            var command = (string?)args?["command"] ?? "";
            return ShellWrites(command, cwd);
        }

        // File tools: every string argument that looks like a path, plus paths named inside a patch.
        var paths = new List<string>();
        Collect(args, paths);
        foreach (var raw in paths)
        {
            foreach (Match m in PatchFile().Matches(raw))
            {
                if (Rel(m.Groups[1].Value.Trim(), cwd) is { } p && IsProtected(p))
                {
                    return p;
                }
            }

            if (!raw.Contains('\n', StringComparison.Ordinal) && Rel(raw, cwd) is { } rel && IsProtected(rel))
            {
                return rel;
            }
        }

        return null;
    }

    private static bool IsProtected(string rel) =>
        rel.StartsWith(Protected, StringComparison.Ordinal) || rel.Contains("/" + Protected, StringComparison.Ordinal);

    private static void Collect(JsonNode? node, List<string> into)
    {
        switch (node)
        {
            case JsonValue value when value.TryGetValue<string>(out var s):
                into.Add(s);
                break;
            case JsonObject obj:
                foreach (var (_, child) in obj)
                {
                    Collect(child, into);
                }

                break;
            case JsonArray arr:
                foreach (var child in arr)
                {
                    Collect(child, into);
                }

                break;
        }
    }

    /// <summary>A shell command that writes under infra/prod/, by pattern. Only what the command names is seen.</summary>
    private static string? ShellWrites(string command, string cwd)
    {
        var c = command.Replace('\\', '/');
        if (!c.Contains("infra/prod", StringComparison.Ordinal) && !c.Contains("prod/main.tf", StringComparison.Ordinal))
        {
            return null;
        }

        return ShellWrite().IsMatch(c) ? "infra/prod/ (a shell command that writes there)" : null;
    }

    private static string? Rel(string path, string cwd)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 400)
        {
            return null;
        }

        var p = path.Replace('\\', '/');
        var root = Repo.Root.Replace('\\', '/').TrimEnd('/') + "/";
        var dir = cwd.Replace('\\', '/').TrimEnd('/') + "/";
        if (p.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return p[root.Length..];
        }

        if (p.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
        {
            return p[dir.Length..];
        }

        return p.TrimStart('.', '/');
    }

    private static void Reply(string decision, string reason) =>
        Console.WriteLine(JsonSerializer.Serialize(new JsonObject
        {
            ["permissionDecision"] = decision,
            ["permissionDecisionReason"] = reason,
        }));

    [GeneratedRegex(@"\*\*\* (?:Update|Add|Delete) File: ([^\n]+)")]
    private static partial Regex PatchFile();

    // Writes: sed -i, perl -pi, redirects, tee, mv/cp/rm/touch, python/node one-liners, PowerShell writes, terraform fmt.
    [GeneratedRegex(@"sed\s+-i|perl\s+-\S*i|>\s*\S*prod|\btee\b|\b(mv|cp|rm|touch|truncate|dd)\b|python3?\s+-|node\s+-e|Set-Content|Add-Content|Out-File|WriteAllText|New-Item|Remove-Item|Copy-Item|Move-Item|terraform\s+fmt|\bed\b|awk\s+-i")]
    private static partial Regex ShellWrite();
}
