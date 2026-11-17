using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tasks.App.Services;

namespace Tasks.App.Pages;

[IgnoreAntiforgeryToken]
public class BulkModel(TaskService tasks, SearchService search) : PageModel
{
    public const int SampleSize = 10_000;

    private static readonly string[] Verbs = ["Write", "Review", "Send", "Book", "Plan", "Check", "Order", "Print"];
    private static readonly string[] Things = ["the report for the team", "an invoice for the client", "the slides for Monday", "a room for the workshop"];

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public int Total { get; private set; }

    public int? Matches { get; private set; }

    public void OnGet()
    {
        Total = tasks.List().Count;
        if (!string.IsNullOrWhiteSpace(Q))
        {
            Matches = search.Count(Q);
        }
    }

    public IActionResult OnPostGenerate()
    {
        for (var i = 0; i < SampleSize; i++)
        {
            tasks.Add(string.Create(CultureInfo.InvariantCulture, $"Sample task {i}: {Verbs[i % Verbs.Length]} {Things[i % Things.Length]}"));
        }

        return RedirectToPage();
    }
}
