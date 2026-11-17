#:project ../../../../src/Tasks.App/Tasks.App.csproj
#:package Microsoft.AspNetCore.Mvc.Testing@10.0.0
#:package Microsoft.Diagnostics.NETCore.Client@0.2.745401
#:package Microsoft.Diagnostics.Tracing.TraceEvent@3.2.6
#:property ManagePackageVersionsCentrally=false
#:property PublishAot=false

// Times a page or endpoint in-process and samples where the CPU time goes. No server, no installs.
//   dotnet run --no-cache .agents/skills/dotnet-diagnostics/scripts/profile.cs -- --setup "POST /Bulk?handler=Generate" --path "/Bulk?q=sample task 12"
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;

internal static class Profile
{
    private static async Task<int> Main(string[] args)
    {
        string? path = null;
        var setup = new List<string>();
        var repeat = 5;
        for (var i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--path": path = args[++i]; break;
                case "--setup": setup.Add(args[++i]); break;
                case "--repeat": repeat = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
            }
        }

        if (path is null)
        {
            Console.WriteLine("Usage: profile.cs -- --path \"/Page?q=x\" [--setup \"POST /Page?handler=X\"] [--repeat 5]");
            return 2;
        }

        var root = FindRoot();
        Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_TASKS_APP", Path.Combine(root, "src", "Tasks.App"));
        Environment.SetEnvironmentVariable("APP_SEED", "false");
        Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Warning");
        Environment.SetEnvironmentVariable("APP_NOW", "2026-11-16T10:00:00+13:00");
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        foreach (var s in setup)
        {
            var r = await Send(client, s);
            Console.WriteLine($"setup {s}: {(int)r.StatusCode}");
        }

        await Send(client, path); // warm-up: JIT and first-request costs aren't the problem we're looking for

        var trace = Path.Combine(Path.GetTempPath(), $"profile-{Environment.ProcessId}.nettrace");
        var providers = new[]
        {
            new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational),
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
        };
        var session = new DiagnosticsClient(Environment.ProcessId).StartEventPipeSession(providers, requestRundown: true);
        var copy = Task.Run(async () =>
        {
            await using var file = File.Create(trace);
            await session.EventStream.CopyToAsync(file);
        });

        var times = new List<double>();
        var status = 0;
        for (var i = 0; i < repeat; i++)
        {
            var sw = Stopwatch.StartNew();
            var r = await Send(client, path);
            times.Add(sw.Elapsed.TotalMilliseconds);
            status = (int)r.StatusCode;
        }

        session.Stop();
        await copy;
        session.Dispose();

        times.Sort();
        Console.WriteLine($"path: {path}");
        Console.WriteLine($"status: {status}");
        Console.WriteLine($"repeat: {repeat}");
        Console.WriteLine($"latency ms: median {times[times.Count / 2]:F1}, max {times[^1]:F1}");
        Console.WriteLine();
        Report(trace);
        File.Delete(trace);
        return 0;
    }

    private static void Report(string trace)
    {
        var etlx = TraceLog.CreateFromEventPipeDataFile(trace);
        using var log = new TraceLog(etlx);
        var inclusive = new Dictionary<string, int>();
        var exclusive = new Dictionary<string, int>();
        var samples = 0;
        foreach (var e in log.Events)
        {
            if (e.ProviderName != "Microsoft-DotNETCore-SampleProfiler")
            {
                continue;
            }

            var frames = new List<string>();
            for (var cs = e.CallStack(); cs is not null; cs = cs.Caller)
            {
                var name = cs.CodeAddress.FullMethodName;
                if (!string.IsNullOrEmpty(name))
                {
                    frames.Add(name);
                }
            }

            // Only samples taken while our request code was on the stack: idle threads aren't the problem.
            if (!frames.Any(f => f.StartsWith("Tasks.App.", StringComparison.Ordinal)))
            {
                continue;
            }

            samples++;
            var deepest = frames.First(f => f.StartsWith("Tasks.App.", StringComparison.Ordinal));
            exclusive[deepest] = exclusive.GetValueOrDefault(deepest) + 1;
            foreach (var f in frames.Distinct())
            {
                if (f.StartsWith("Tasks.App.", StringComparison.Ordinal))
                {
                    inclusive[f] = inclusive.GetValueOrDefault(f) + 1;
                }
            }
        }

        Console.WriteLine($"CPU samples in app code: {samples}");
        Console.WriteLine("app methods by inclusive time (share of samples with the method on the stack):");
        foreach (var (name, n) in inclusive.OrderByDescending(kv => kv.Value).Take(6))
        {
            Console.WriteLine($"  {100.0 * n / Math.Max(1, samples),5:F1}%  {name}");
        }

        Console.WriteLine("app methods by own time (the deepest app method on the stack: look here first):");
        foreach (var (name, n) in exclusive.OrderByDescending(kv => kv.Value).Take(5))
        {
            Console.WriteLine($"  {100.0 * n / Math.Max(1, samples),5:F1}%  {name}");
        }
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string request)
    {
        var post = request.StartsWith("POST ", StringComparison.Ordinal);
        var url = request.StartsWith("GET ", StringComparison.Ordinal) || post ? request[(request.IndexOf(' ', StringComparison.Ordinal) + 1)..] : request;
        url = url.Replace(" ", "%20", StringComparison.Ordinal);
        return post ? client.PostAsync(url, null) : client.GetAsync(url);
    }

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TasksLab.slnx")))
            {
                return dir.FullName;
            }
        }

        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TasksLab.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Run this from inside the repo.");
    }
}
