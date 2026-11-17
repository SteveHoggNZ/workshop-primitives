using System.Diagnostics;
using System.Text;

namespace Lab;

/// <summary>Runs external commands and finds them on PATH (including .cmd/.exe on Windows).</summary>
internal static class Proc
{
    public sealed record Result(int ExitCode, string Output);

    public static string? Find(string name)
    {
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';')
            : [""];
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir, name + ext.ToLowerInvariant());
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                candidate = Path.Combine(dir, name + ext);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    public static Result Run(string file, IEnumerable<string> args, int timeoutMs = 120_000)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Repo.Root,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        // English output (lint parses it), no telemetry or banners, no lingering build nodes.
        psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";

        var output = new StringBuilder();
        Process process;
        try
        {
            process = Process.Start(psi) ?? throw new InvalidOperationException($"Couldn't start {file}");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return new Result(-1, ex.Message);
        }

        using (process)
        {
            process.OutputDataReceived += (_, e) => Append(output, e.Data);
            process.ErrorDataReceived += (_, e) => Append(output, e.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (!process.WaitForExit(timeoutMs))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // already gone
                }

                return new Result(-1, output + $"(timed out after {timeoutMs / 1000}s)");
            }

            process.WaitForExit();
            lock (output)
            {
                return new Result(process.ExitCode, output.ToString());
            }
        }
    }

    private static void Append(StringBuilder output, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(line);
        }
    }
}
