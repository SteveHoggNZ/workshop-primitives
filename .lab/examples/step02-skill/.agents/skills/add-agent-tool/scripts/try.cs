#:project ../../../../src/Tasks.App/Tasks.App.csproj
#:property PublishAot=false

// Sends messages to the in-app agent against a fresh, seeded task list and prints the conversation.
//   dotnet run --no-cache .agents/skills/add-agent-tool/scripts/try.cs -- "help" "list"
using Tasks.App.Agent;
using Tasks.App.Data;
using Tasks.App.Services;

internal static class TryAgent
{
    private static void Main(string[] args)
    {
        var store = new TaskStore();
        SeedData.Fill(store, TimeProvider.System);
        var agent = new ScriptedAgent(new TaskService(store, TimeProvider.System));
        foreach (var message in args)
        {
            Console.WriteLine($"> {message}");
            Console.WriteLine(agent.Handle(message));
            Console.WriteLine();
        }
    }
}
