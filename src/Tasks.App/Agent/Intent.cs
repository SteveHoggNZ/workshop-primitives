using System.Text.RegularExpressions;

namespace Tasks.App.Agent;

/// <summary>One command the scripted agent understands: a pattern and the tool it calls.</summary>
/// <param name="Name">Short name for the command.</param>
/// <param name="Pattern">What the user types. Named groups become the tool's arguments.</param>
/// <param name="Tool">Key of the tool in <see cref="AgentTools.All"/>.</param>
/// <param name="Usage">How the command is written in help, for example <c>done &lt;id&gt;</c>.</param>
/// <param name="Example">An example for help.</param>
/// <param name="Confirm">Ask "Type yes to confirm" before calling the tool.</param>
/// <param name="Preview">Optional text for the confirm prompt ("This will ..."). Defaults to "run '&lt;usage&gt;'".</param>
public sealed record Intent(
    string Name,
    Regex Pattern,
    string Tool,
    string Usage,
    string Example,
    bool Confirm = false,
    AgentTool? Preview = null);
