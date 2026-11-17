using Tasks.App.Models;

namespace Tasks.App.Data;

/// <summary>Sample tasks so the pages have something to show.</summary>
public static class SeedData
{
    public static void Fill(TaskStore store, TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        string[] titles =
        [
            "Book the venue", "Draft the agenda", "Order coffee",
            "Send invitations", "Print name badges", "Test the projector",
        ];
        for (var i = 0; i < titles.Length; i++)
        {
          var created = now.AddHours(-(titles.Length - i) * 3);   
          DateTimeOffset? completed = i < 2 ? created.AddHours(1) : null;
            store.Add(new TaskItem($"seed{i + 1}", titles[i], created, completed));  
        }
    }
}
