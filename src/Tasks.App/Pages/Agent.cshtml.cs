using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tasks.App.Agent;

namespace Tasks.App.Pages;

public class AgentModel(ScriptedAgent agent) : PageModel
{
    [BindProperty]
    public string Message { get; set; } = "";

    public IReadOnlyList<AgentTurn> Conversation => agent.Conversation;

    public IActionResult OnPost()
    {
        if (!string.IsNullOrWhiteSpace(Message))
        {
            agent.Handle(Message);
        }

        return RedirectToPage();
    }
}
