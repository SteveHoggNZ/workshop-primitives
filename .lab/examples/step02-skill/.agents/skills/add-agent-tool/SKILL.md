---
name: add-agent-tool
description: "Add a new command to the in-app task agent at /Agent: the tool, its intent row, confirmation, a test, the docs row and a live check. Use when asked to add an agent command or something users can type at /Agent. Don't use it to change an existing command or to build a page."
---

# Add an in-app agent command

A command is done only when all six steps are done. Do them in order.

## 1. Tool: `src/Tasks.App/Agent/AgentTools.cs`

- Add `public static string <Name>(TaskService tasks, IReadOnlyDictionary<string, string> args)` with a one-line `<summary>`.
- Use `TaskService` only, never `TaskStore`. If `TaskService` has no method for it, add one.
- The reply is one short sentence. **When a command affects more than one task, say how many:**
  "Deleted 3 completed tasks." Users can't see the list while they type, so the count is the confirmation.

## 2. Register it: same file

- Add `["<tool_name>"] = <Name>` to `AgentTools.All`.

## 3. Intent row: `src/Tasks.App/Agent/AgentIntents.cs`

- Add a row to `AgentIntents.All`: name, pattern from `P(...)`, tool name, usage and example.
- **Anything that deletes or changes more than one task sets `Confirm: true`.** `ScriptedAgent` then asks
  "Type yes to confirm" before it calls the tool. This is a team rule, not a suggestion.
- **It also sets `Preview`, and the preview says how many:** "This will delete 2 completed tasks. Type yes to
  confirm." The default prompt ("This will run 'clear completed'") doesn't tell users what they're agreeing to.
  `Preview` takes the same arguments as a tool and returns the words after "This will":

  ```csharp
  Preview: (tasks, _) => $"delete {tasks.Completed().Count} completed tasks"
  ```
- `help` is built from `AgentIntents.All`, so it updates itself.

## 4. Test: `tests/Tasks.App.Tests/AgentTests.cs`

- Drive the command through `ScriptedAgent.Handle(...)`, as the existing tests do.
- For a command that confirms, test both "yes" and "no".

## 5. Docs: `docs/agent-commands.md`

- Add a row to the table: command, tool, confirms first, example.

## 6. Try it

Run the skill's script. It sends messages to the agent against a fresh, seeded task list:

```sh
dotnet run --no-cache .agents/skills/add-agent-tool/scripts/try.cs -- "help" "<your command>" "yes" "list"
```

Keep `--no-cache`: without it the script can reuse an old build and show the agent as it was before your change.

**Paste the script's output, unchanged, in a code block in your final message.** A summary ("the try script
confirmed it") doesn't count: the reviewer reads the conversation to check the prompt and the reply wording.

Then run `dotnet test`.
