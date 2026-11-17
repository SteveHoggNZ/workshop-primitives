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
        if (sub == "gate")
        {
            return Gate.Run(args[1..]);
        }

        if (sub != "guard")
        {
            Console.Error.WriteLine("Usage: ./lab hook guard [--bar] [--sleep N] | gate   (reads a hook event as JSON on stdin)");
            return 2;
        }

        var bar = args.Contains("--bar");

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

        if (bar && TouchesBar(tool, toolArgs, cwd) is { } barHit)
        {
            Reply("deny",
                $"{barHit} sets the lint bar. People move the bar, not an agent and not to pass a check. Leave it as it is and fix " +
                "the findings in the code instead.");
            return 0;
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
        var shell = IsShell(tool);
        if (shell)
        {
            var command = (string?)args?["command"] ?? "";
            return ShellWrites(command, cwd);
        }

        // Reading is always fine: only tools that create, change or delete files are judged.
        if (!WriteTool().IsMatch(tool))
        {
            return null;
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

    /// <summary>A write to one of the files that set the lint bar (see Gate.Bar), or null.</summary>
    private static string? TouchesBar(string tool, JsonNode? args, string cwd)
    {
        var shell = IsShell(tool);
        if (shell)
        {
            var c = ((string?)args?["command"] ?? "").Replace('\\', '/');
            return Gate.Bar.FirstOrDefault(b => c.Contains(Path.GetFileName(b), StringComparison.Ordinal)) is { } f && ShellWrite().IsMatch(c) ? f : null;
        }

        if (!WriteTool().IsMatch(tool))
        {
            return null;
        }

        var paths = new List<string>();
        Collect(args, paths);
        foreach (var raw in paths)
        {
            var candidates = PatchFile().Matches(raw).Select(m => m.Groups[1].Value.Trim()).ToList();
            if (!raw.Contains('\n', StringComparison.Ordinal))
            {
                candidates.Add(raw);
            }

            foreach (var cand in candidates)
            {
                if (Rel(cand, cwd) is { } rel && Gate.Bar.FirstOrDefault(b => rel == b || rel.EndsWith("/" + b, StringComparison.Ordinal)) is { } hit)
                {
                    return hit;
                }
            }
        }

        return null;
    }

    private static bool IsProtected(string rel) =>
        rel.StartsWith(Protected, StringComparison.OrdinalIgnoreCase) || rel.Contains("/" + Protected, StringComparison.OrdinalIgnoreCase);

    // Copilot CLI: bash, powershell. VS Code: run_in_terminal. Claude-style payloads: Bash, PowerShell.
    private static bool IsShell(string tool) => tool is "bash" or "shell" or "powershell" or "Bash" or "PowerShell" or "run_in_terminal";

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

        return ShellWrite().IsMatch(c) ? "infra/prod/main.tf (or another file there, from the shell)" : null;
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

    [GeneratedRegex(@"edit|write|create|patch|replace|insert|delete|remove|move|rename|notebook", RegexOptions.IgnoreCase)]
    private static partial Regex WriteTool();

    [GeneratedRegex(@"\*\*\* (?:Update|Add|Delete) File: ([^\n]+)")]
    private static partial Regex PatchFile();

    // Writes that name infra/prod as their target: sed -i / perl -pi on it, a redirect or tee into it, mv/cp/rm/touch
    // naming it, PowerShell writes, python/node one-liners that open it for writing, terraform fmt there. Reads
    // (cat, git diff, a redirect to /tmp) don't match. It's still pattern matching on the command line: see Step 6.
    [GeneratedRegex(@"(sed|perl)\s+-\S*i[^|;&]*infra/prod|>>?\s*\S*infra/prod|\btee\s+(-a\s+)?\S*infra/prod|\b(mv|cp|rm|touch|truncate)\b[^|;&]*infra/prod|(Set-Content|Add-Content|Out-File|New-Item|Remove-Item|Copy-Item|Move-Item)[^|;&]*infra/prod|WriteAllText[^|;&]*infra/prod|(python3?|node)\b[^|;&]*infra/prod[^|;&]*['""](w|a|r\+)['""]|(python3?|node)\b[^|;&]*(write|writeFile)[^|;&]*infra/prod|(cd\s+\S*infra/prod\S*\s*(&&|;)\s*)(sed\s+-\S*i|perl\s+-\S*i|terraform\s+fmt|echo[^|;&]*>|tee\b)|terraform\s+fmt[^|;&]*infra/prod", RegexOptions.IgnoreCase)]
    private static partial Regex ShellWrite();
}
