# Step 2 walkthrough: a skill with a tested script

Every step is an experiment: run a prompt **without** the primitive, install it, run the **same**
prompt again in a new session, and compare. If you install first, there's nothing to compare against.

**Before you start:** Steps 0 and 1 must be installed. Check with:

```bash
./lab list
```

You should see an `x` next to steps 00 and 01:

```text
[x] 00  lint
[x] 01  instructions
[ ] 02  skill
```

If not, run `./lab goto 2` to install them. Then open `/Agent` once (`dotnet run --project src/Tasks.App`)
and type `help` and `delete seed1`, so you know what the in-app agent does today.

1. **Before.** Start a new agent session (don't install anything yet) and give it this prompt:

   ```text
   Add a command to the in-app agent at /Agent that deletes all completed tasks. Users should be able to type "clear completed".
   ```

   Score it with the checklist below. Don't paste the checklist into the prompt.
2. **Reset.** `./lab reset` discards the Before changes.
3. **Install.** `./lab apply 2` copies the skill and a prompt file in and commits them as `lab: step02 skill`.
4. **After.** Start a **new** session, give it the same prompt, and score it again.
5. **Compare** the two scores, then read on.

## Checklist (one point each)

| # | The result... |
|---|---|
| 1 | "clear completed" deletes the completed tasks and leaves the open ones |
| 2 | asks first, and answering "no" keeps the tasks |
| 3 | says how many tasks will go **before** deleting: "This will delete 2 completed tasks. Type yes to confirm." |
| 4 | adds a test that types "clear completed", and `dotnet test` passes |
| 5 | adds the command to `docs/agent-commands.md` |
| 6 | shows a live run in the agent's final message: `> clear completed` and the "Type yes to confirm" reply |

> The rest of this page explains what to look for. Read it after your Before run.

## Quick links

**The primitive**
- [.agents/skills/add-agent-tool/SKILL.md](../examples/step02-skill/.agents/skills/add-agent-tool/SKILL.md): the team's procedure, loaded only when the task matches its description
- [.agents/skills/add-agent-tool/scripts/try.cs](../examples/step02-skill/.agents/skills/add-agent-tool/scripts/try.cs): the script the skill runs to try the agent
- [.github/prompts/new-agent-tool.prompt.md](../examples/step02-skill/.github/prompts/new-agent-tool.prompt.md): the same job as a prompt file you run yourself (see Try)

**Code the agent reads**
- [Agent/AgentIntents.cs](../../src/Tasks.App/Agent/AgentIntents.cs): the command table, including `delete` with `Confirm: true`
- [Agent/AgentTools.cs](../../src/Tasks.App/Agent/AgentTools.cs): the tools the commands call
- [Agent/ScriptedAgent.cs](../../src/Tasks.App/Agent/ScriptedAgent.cs#L51-L86): matching, the confirm prompt and "Cancelled."
- [Agent/Intent.cs](../../src/Tasks.App/Agent/Intent.cs): one command, including the optional `Preview` for the confirm prompt
- [docs/agent-commands.md](../../docs/agent-commands.md): the list of commands
- [tests/Tasks.App.Tests/AgentTests.cs](../../tests/Tasks.App.Tests/AgentTests.cs): how the agent is tested

<details>
<summary>Where to check the result (spoilers)</summary>

- `src/Tasks.App/Agent/AgentIntents.cs` (the new row, `Confirm: true` and `Preview`) and `AgentTools.cs`
- `src/Tasks.App/Services/TaskService.cs` (a delete-completed method; no `TaskStore` outside it)
- `tests/Tasks.App.Tests/AgentTests.cs`
- `docs/agent-commands.md`
- The session transcript or final message (the live run)

</details>

## What this step shows

Step 1 put team decisions in `AGENTS.md`, which loads in every session. Some knowledge only matters
for one kind of task: here, how the team adds an agent command. Putting it in `AGENTS.md` costs context
in every session that never touches the agent. A **skill** loads only when its description matches the
task. It can also carry a **script**, so the agent can show the change working instead of saying so.

What happened when we ran it (Copilot CLI, strong model for the Before, small model for the After):

| Item | Before (strong) | After (small, with the skill) |
|---|---|---|
| 1 clears only completed | ✓ ✓ | ✓ ✓ |
| 2 asks first, "no" cancels | ✓ ✓ | ✓ ✓ |
| 3 confirm says how many | · · | ✓ ✓ |
| 4 test, suite passes | ✓ ✓ | ✓ ✓ |
| 5 docs row | · · | ✓ ✓ |
| 6 live run in the final message | · · | ✓ ✓ |
| **Score** | **3/6, 3/6** | **6/6, 6/6** |

The strong model gets everything the code shows. It even found the unused `Preview` hook and wrote
"This will delete all completed tasks". It missed what only the team knows: the count, the docs
file, and showing it working.

## What the code already teaches

An agent asked for a new command reads `AgentIntents.cs`, `AgentTools.cs` and `ScriptedAgent.cs`.

- **The shape of a command.** A tool in [`AgentTools`](../../src/Tasks.App/Agent/AgentTools.cs#L11-L17)
  and a row in [`AgentIntents`](../../src/Tasks.App/Agent/AgentIntents.cs#L8-L14). Copying `delete`
  gives a working command, so item 1 is easy.
- **Confirmation exists.** `delete` has `Confirm: true`, and `ScriptedAgent` [asks first](../../src/Tasks.App/Agent/ScriptedAgent.cs#L75-L80).
  A model that copies `delete` gets item 2 for free. What it can't see is that this is a *rule* for
  anything that deletes more than one task.
- **Tests.** [`AgentTests`](../../tests/Tasks.App.Tests/AgentTests.cs) drive `ScriptedAgent.Handle`.
  A strong model adds one, so item 4 is usually a pass.

## What the code can't teach

### The count in the confirm prompt (item 3)

- **Where it applies:** the question before deleting. Copying `delete` gives the default prompt,
  "This will run 'clear completed'. Type yes to confirm.", which doesn't say what you're agreeing to.
- **The hook is there, unused.** [`Intent.Preview`](../../src/Tasks.App/Agent/Intent.cs#L12) changes the
  prompt, but no command sets it, so nothing in the code says when you must. Like Step 1's `_Empty` partial.
- **Closing rule:** [set `Preview`, and say how many](../examples/step02-skill/.agents/skills/add-agent-tool/SKILL.md#L26-L32).
  The *why* is in the skill: users can't see the list while they type.
- **Why not the reply after deleting?** We tried that first. A strong model writes "Deleted 2 completed
  tasks." without being told, so it didn't separate the runs. The skill still asks for it
  ([step 1](../examples/step02-skill/.agents/skills/add-agent-tool/SKILL.md#L14-L15)), but it isn't scored.

### The docs row (item 5)

- **Where it applies:** [docs/agent-commands.md](../../docs/agent-commands.md). Nothing in the code
  or the other docs links to it. The doc says "keep this table in step", but only an agent that opens it sees that.
- **Typical Before:** a strong model that lists `docs/` may still find it. In our first design the
  architecture doc linked to it, and every Before added the row.
- **Closing rule:** [step 5 of the skill](../examples/step02-skill/.agents/skills/add-agent-tool/SKILL.md#L40-L42).

### The live run (item 6): the script

- **Where it applies:** the final message. A Before says "tests pass". The After pastes a conversation.
- **Closing rule:** [step 6 of the skill](../examples/step02-skill/.agents/skills/add-agent-tool/SKILL.md#L44-L57)
  runs [try.cs](../examples/step02-skill/.agents/skills/add-agent-tool/scripts/try.cs), a .NET 10
  file-based app (`dotnet run file.cs`) that references the app project and drives `ScriptedAgent`
  against the seed data.
- **Why a script, not "run the app":** it's one command with fixed output, so the agent runs it the same
  way every time and you can read the result. A browser check needs a server, a port and a form post.
- **Ask for the paste, in bold.** Our first skill said "paste its output". Both Afters ran the script
  and then wrote "the try script confirmed it". Models trim final messages. The skill now says a summary
  doesn't count, and why: the reviewer reads the conversation.
- **Why `--no-cache`:** a file-based app caches its build and doesn't notice changes in the project it
  references. Without the flag the script shows the agent as it was *before* the change. We found
  this while building the step. A tested script means testing it on the edit loop, not just once.

## Reading the files

[SKILL.md](../examples/step02-skill/.agents/skills/add-agent-tool/SKILL.md) (57 lines):

- [The description](../examples/step02-skill/.agents/skills/add-agent-tool/SKILL.md#L3): what the skill
  does, when to use it, and when **not** to ("Don't use it to change an existing command or to build a
  page"). The agent sees only this line until it decides to load the skill.
- Six numbered steps, each naming the file. "Done only when all six steps are done" is what gets the
  docs row and the live run.
- Each rule that isn't obvious says why: users can't see the list; confirming is a team rule.
- It shows the one line of code that's easy to get wrong (`Preview`), not the whole change.

What to notice:

- **The description is the trigger.** A vague one ("helps with the agent") loads too often or never.
- **Say when not to.** Without the "don't" sentence, "rename the delete command" would load a
  procedure for adding a new one.
- **The script is part of the skill.** It ships in the skill's folder and is run from it, so the
  procedure and its check travel together.

## Did it load?

- **Strongest evidence:** the final message has a conversation starting `> help`. Nothing else asks for it.
- **Copilot CLI:** the transcript shows a `skill` call for `add-agent-tool`, or the agent reading `SKILL.md`.
- **VS Code:** the response's references list `SKILL.md`.

## Scoring: where to look

| Item | Where to check |
|---|---|
| 1 | Type `clear completed` then `yes` at `/Agent`, then `list`: only open tasks are left |
| 2 | Type `clear completed` then `no`: "Cancelled." and nothing deleted. In code: `Confirm: true` on the row |
| 3 | The reply to `clear completed`: "This will delete 2 completed tasks. Type yes to confirm." with the seed data |
| 4 | `tests/Tasks.App.Tests/AgentTests.cs` has a test that sends "clear completed"; `dotnet test` passes |
| 5 | `docs/agent-commands.md` has a `clear completed` row |
| 6 | The final message. It leaves no trace in `git diff` |

The reference solution is [.lab/solutions/step02.patch](../solutions/step02.patch)
(`./lab goto 3 --solutions` installs it).

## If the After didn't improve

- **Old session.** Skills are discovered at session start; start a new one.
- **Not installed.** `./lab list` shows `[x] 02  skill`, and `.agents/skills/add-agent-tool/SKILL.md` exists.
- **The skill didn't load.** Check the transcript (see "Did it load?"). If it never loaded, the
  description didn't match the prompt. That's the lesson: rewrite the description, not the prompt.
- **Personal skills.** User-level skills change both runs; turn them off.

## Try

1. **Human-invoked vs model-invoked.** In a new session (after `./lab reset`), run the prompt file
   instead: `/new-agent-tool` in VS Code chat, with "delete all open tasks" and "clear open". The prompt
   file runs because *you* chose it, and it tells the agent to use the skill. The skill alone runs
   when the *model* decides the task matches. Use a prompt file for a job you start on purpose; use a
   skill for knowledge the agent should find by itself.
2. **When not to.** Ask a new session: "Change the delete command's reply to include the task title."
   Does the skill load? It shouldn't: the description says it's not for changing an existing command.
   Then delete the "Don't use it..." sentence and try again.
3. **Break the script.** Run `try.cs` without `--no-cache`, change the help text in `ScriptedAgent.cs`,
   and run it again. The old text comes back.

## For us

- [ ] Before/After by hand in VS Code (Steve)
- [x] **Validated with the harness** (Copilot CLI 1.0.112, 17 Nov 2026), 2 runs per arm, no flips:
  Before (gpt-6.1-sol) **3/6, 3/6**; After (gpt-5.6-luna) **6/6, 6/6**. Both Afters loaded the skill and
  ran `try.cs`. Took three rounds: v1 (count in the reply, a docs link in architecture.md) didn't separate
  the arms, and the v2 skill let the After summarise the script instead of pasting it. Details in
  `.lab/validation/NOTES.md`. Scorer: `.lab/scripts/steps/step02.py` with hidden checks
  `.lab/scripts/checks/LabCheck02.cs`.
- [x] Scorer calibrated: reference solution 6/6, untouched clone 0/6
- [x] Reference solution passes the tests and leaves lint at 6; `goto 3 --solutions` applies it
- [ ] Claude Code adapter: Claude Code reads skills from `.claude/skills/`, so `--tools claude` needs a
  copy or pointer there (not built yet)
