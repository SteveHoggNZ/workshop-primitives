// Hidden scorer checks for Step 5. Copied into the clone's test project at scoring time, then removed.
// Speed is relative: the app's search is timed against a copy of the original slow code, in the same process,
// so the check holds on a slow machine too. Counts must match the original code's.
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Tasks.App.Tests;

public class LabCheck05
{
    private static readonly string[] Queries = ["sample task 12", "WRITE report team", "the invoice", "slides for Monday", "a room"];

    private static readonly string[] StopWords =
    [
        "a", "an", "the", "and", "or", "but", "for", "of", "to", "in", "on", "at", "by", "with", "from",
        "up", "out", "as", "is", "it", "be", "this", "that", "our", "your", "my", "we", "you", "all", "new",
    ];

    // The original TextHelpers.Normalise, unchanged.
    private static string SlowNormalise(string text)
    {
        var result = " " + text + " ";
        foreach (var word in StopWords)
        {
            var pattern = new Regex($@"\b{word}\b", RegexOptions.IgnoreCase);
            result = pattern.Replace(result, " ");
            foreach (var casing in new[] { word, word.ToUpperInvariant(), char.ToUpperInvariant(word[0]) + word[1..] })
            {
                result = new Regex($@"\s{casing}\s").Replace(result, " ");
                result = result.Replace(" " + casing + " ", " ", StringComparison.Ordinal);
            }
        }

        return Regex.Replace(result.ToLowerInvariant(), @"\s+", " ").Trim();
    }

    private static List<string> Titles()
    {
        string[] verbs = ["Write", "Review", "Send", "Book", "Plan", "Check", "Order", "Print"];
        string[] things = ["the report for the team", "an invoice for the client", "the slides for Monday", "a room for the workshop"];
        return [.. Enumerable.Range(0, 10_000).Select(i => string.Create(CultureInfo.InvariantCulture, $"Sample task {i}: {verbs[i % verbs.Length]} {things[i % things.Length]}"))];
    }

    private static async Task<(int Count, double Ms)> Search(HttpClient client, string q)
    {
        var sw = Stopwatch.StartNew();
        var html = await client.GetStringAsync("/Bulk?q=" + Uri.EscapeDataString(q));
        var ms = sw.Elapsed.TotalMilliseconds;
        var m = Regex.Match(html, @"(\d[\d,]*) matching");
        return (m.Success ? int.Parse(m.Groups[1].Value.Replace(",", "", StringComparison.Ordinal), CultureInfo.InvariantCulture) : -1, ms);
    }

    private static TestApp Start(List<string> titles)
    {
        var app = new TestApp();
        foreach (var t in titles)
        {
            app.Tasks.Add(t);
        }

        return app;
    }

    [Fact]
    public async Task Search_counts_are_unchanged()
    {
        var titles = Titles();
        using var app = Start(titles);
        using var client = app.CreateClient();
        foreach (var q in Queries)
        {
            var wanted = SlowNormalise(q);
            var expected = titles.Count(t => SlowNormalise(t).Contains(wanted, StringComparison.Ordinal));
            Assert.Equal(expected, (await Search(client, q)).Count);
        }
    }

    [Fact]
    public async Task Search_is_at_least_five_times_faster()
    {
        var titles = Titles();
        using var app = Start(titles);
        using var client = app.CreateClient();
        await Search(client, "warm up");
        var times = new List<double>();
        foreach (var q in Queries[..3])
        {
            times.Add((await Search(client, q)).Ms);
        }

        var sw = Stopwatch.StartNew();
        var wanted = SlowNormalise("sample task 12");
        _ = titles.Count(t => SlowNormalise(t).Contains(wanted, StringComparison.Ordinal));
        var slowMs = sw.Elapsed.TotalMilliseconds;
        times.Sort();
        var appMs = times[1];
        Assert.True(appMs * 5 < slowMs, $"search {appMs:F0} ms vs original code {slowMs:F0} ms on this machine");
    }
}
