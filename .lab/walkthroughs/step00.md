# Step 0 walkthrough: setup, and a warm-up with a linter

Step 0 does two things. It gets the machine ready, and it runs the lab's loop once on something small
and visual: **a linter**. You ask a fresh agent what's wrong with a page, turn the lint rules on, and
ask again in a new session. The difference is the lesson: before, the agent can only judge the code
by reading it; after, it can read deterministic diagnostics that a tool decided.

Every later step uses the same loop, so this is also the practice run.

## Part A: get ready

1. **Check the machine.** From the repo root:

   ```bash
   ./lab doctor          # Windows: lab doctor
   ```

   The first run builds the helper into `.lab-bin/` (a few seconds). Every required line should say
   `OK`, and the last line `Ready: the core steps can run on this machine.` Optional tools (node,
   Bicep, copilot) can show `--`; the line under each says which steps need them.
2. **Run the tests.**

   ```bash
   dotnet test
   ```

   Expect `Passed! - Failed: 0, Passed: 4`, with two `warning CS8618` lines on the way.
3. **Run the app.** `dotnet run --project src/Tasks.App`, open the URL it prints, then `/Recent`.
4. **Open the folder in VS Code** (with the C# and C# Dev Kit extensions) and **wait** until the
   status bar shows `Projects: Tasks.App` and the spinner stops, about 40 seconds. Don't open any `.cs`
   file before then (see "If it doesn't match").
5. **Look at the starting point.** **View → Problems** shows **2** warnings, both `CS8618` in
   `Recent.cshtml.cs`. `./lab lint` says `Total: 2`. Only the compiler's own nullable check is on.

## Part B: the loop

1. **Before.** Open a **new** Copilot chat in agent mode and send:

   ```text
   What problems are there in src/Tasks.App/Pages/Recent.cshtml.cs?
   ```

   Note what it lists. Score it with the checklist below, but don't paste the checklist into the prompt.
2. **Reset and install.**

   ```bash
   ./lab reset        # discards anything the Before run changed (here, probably nothing)
   ./lab apply 0      # installs this step's files as their own commit
   ```

   `apply` adds two files and commits them as `lab: step00 lint`:
   [src/.editorconfig](../examples/step00-lint/src/.editorconfig) (four analyzer rules turned on)
   and [docs/lint-baseline.md](../examples/step00-lint/docs/lint-baseline.md) (`Total: 6`).
   `./lab list` now shows `[x] 00  lint`.
3. **Check it took.** Within a few seconds the Problems panel shows **6** warnings, and `./lab lint`
   says `Total: 6 (baseline 6)`. If the panel doesn't update, reload the window.
4. **After.** Start **another new** chat (the old one keeps what it already concluded) and send the same
   prompt.
5. **Compare** the two answers.

## Part C: navigate by meaning (showcase, not scored)

Linting and types let the agent **see** problems. A language server lets it **find its way**: "who
calls this?" answered by the compiler's understanding of the code, not by matching text. VS Code
already runs one for C# (the C# extension), so there's nothing to install.

1. **Ask.** In a **new** chat:

   ```text
   Which code calls TaskService.Recent()? List each call site with its file and line.
   ```

   Expand the response's tool calls: did it use a text search, or a references/usages lookup?
2. **Do it both ways yourself.** Search the workspace for `Recent(` (Cmd+Shift+F): six hits. Then open
   [TaskService.cs](../../src/Tasks.App/Services/TaskService.cs#L33), put the cursor on `Recent` and
   press **Shift+F12** (Find All References): just the real ones.
3. **Ask again, naming the tool.** In another new chat, add one line to the same prompt:

   ```text
   Which code calls TaskService.Recent()? List each call site with its file and line.
   Use #usages to find references rather than text search.
   ```

   Same answer? Fewer files read? This one line is the kind of rule Step 1 puts in `AGENTS.md`.

> The rest of this page explains what to look for. Read it after your Before run.

## Checklist

| # | The answer... | Before (typical) | After |
|---|---|---|---|
| 1 | names the culture-dependent `DateTime.ToString("HH:mm:ss")` (CA1305) | sometimes, as "uses local time" | yes |
| 2 | names `ToUpper()` without a culture (CA1304) | rarely | yes |
| 3 | names `Count()` where `.Count` exists (CA1829) | rarely | yes |
| 4 | names `Describe` could be `static` (CA1822) | rarely | yes |
| 5 | says where the findings came from: the Problems panel / diagnostics, or `./lab lint` | no, it read the code | yes |

**Where to check item 5:** expand the chat response's tool calls. In VS Code agent mode, look for a
problems or errors check (it reads the same diagnostics as the Problems panel). In the Copilot CLI,
look for `dotnet build` or `./lab lint` in the session.

## What this step shows

**A linter is a primitive.** It's the cheapest one in the lab: a rule a tool can decide, written down
once, checked the same way every time, and visible to you and the agent alike. Before, the agent has
nothing to read except the code, so it gives a judgement: plausible, varied, and different each run.
After, it reads diagnostics, so its list matches yours.

**What both runs find anyway:** `Tasks.First()` throws when there are no tasks, the page reads the
system clock with `DateTime.Now`, and it uses `TaskStore` directly instead of `TaskService`. No linter
rule here catches those. They're decisions and design, which is what Step 1's instructions and later
review are for. **The lesson isn't "linters find everything". It's "let a tool decide what a tool can
decide, and keep the agent's judgement for the rest."**

That's also why the lint count matters later. Step 1 asks the agent to report it, and Step 7 refuses
to let it go up.

## What Part C shows

The question has a trap: two classes have a `Recent()` method, and one real caller goes through an
interface.

| Text search hit for `Recent(` | Calls `TaskService.Recent()`? |
|---|---|
| [TaskServiceTests.cs L26](../../tests/Tasks.App.Tests/TaskServiceTests.cs#L26) `app.Tasks.Recent()` | **Yes** |
| [Program.cs L35](../../src/Tasks.App/Program.cs#L35) `queries.Recent()` | **Yes**, through `ITaskQueries`, which `TaskService` implements. No "TaskService" on that line |
| [Index.cshtml.cs L19](../../src/Tasks.App/Pages/Index.cshtml.cs#L19) `feed.Recent(3)` | **No**: that's `ActivityFeed.Recent()`, a different method with the same name |
| [TaskService.cs L33](../../src/Tasks.App/Services/TaskService.cs#L33), [ITaskQueries.cs L10](../../src/Tasks.App/Services/ITaskQueries.cs#L10), [ActivityFeed.cs L6](../../src/Tasks.App/Services/ActivityFeed.cs#L6) | No: declarations |

A strong model reading carefully usually gets this right from text search too; it just reads more
files to get there, and a smaller model may count `feed.Recent(3)` or miss the interface call. Find All
References knows the types, so it's exact in one step.

The honest lesson, from the Python lab's runs: **agents rarely pick the semantic tool unprompted**.
The capability is there; one line of instruction makes it the default. That's a theme for the rest of
the lab: a primitive often isn't a new capability, it's telling the agent to use the one it has.

**Copilot CLI:** the CLI needs a language server configured in `.github/lsp.json` to get an `lsp`
tool. Choosing a C# server that installs without a global tool is open spike S7; until then Part C is
VS Code only.

## The six findings

| Line | Rule | What it says | Why it's planted |
|---|---|---|---|
| [L11](../../src/Tasks.App/Pages/Recent.cshtml.cs#L11) | CS8618 | `Tasks` may be null after the constructor | Compiler nullable warning: on in both runs |
| [L11](../../src/Tasks.App/Pages/Recent.cshtml.cs#L11) | CS8618 | Same for `Labels` | Two findings on one line, so the count isn't "one per line" |
| [L29](../../src/Tasks.App/Pages/Recent.cshtml.cs#L29) | CA1829 | `Count()` where `.Count` exists | A rule a linter can enforce and an instruction shouldn't need to |
| [L37](../../src/Tasks.App/Pages/Recent.cshtml.cs#L37) | CA1304 | `ToUpper()` with no culture | Culture bugs: the same family as the time-zone rule in Step 1 |
| [L38](../../src/Tasks.App/Pages/Recent.cshtml.cs#L38) | CA1305 | `DateTime.ToString(format)` with no culture | Also the line that reads the system clock: a Step 1 trap |
| [L41](../../src/Tasks.App/Pages/Recent.cshtml.cs#L41) | CA1822 | `Describe` could be `static` | Harmless style debt, the kind a quality gate ratchets |

Compare [Index.cshtml.cs](../../src/Tasks.App/Pages/Index.cshtml.cs), which uses `TaskService` and
has no findings.

## Quick links

**The primitive**
- [src/.editorconfig](../examples/step00-lint/src/.editorconfig): the four rules
- [docs/lint-baseline.md](../examples/step00-lint/docs/lint-baseline.md): what the count is compared against

**What makes the count stable**
- [Directory.Build.props](../../Directory.Build.props#L15-L19): every analyzer rule off unless named; level pinned
- [tools/Lab/Lint.cs](../../tools/Lab/Lint.cs): what `./lab lint` runs and how it counts

**Setup**
- [global.json](../../global.json): SDK 10.0.100 or later; newer SDKs allowed
- [tools/Lab/Doctor.cs](../../tools/Lab/Doctor.cs): what `./lab doctor` checks

## The helper commands

| Command | What it does |
|---|---|
| `./lab list` | The steps, which are installed (`x`), and what's next |
| `./lab reset [--dry-run]` | Throws away uncommitted changes in the lab's folders (`src/`, `tests/`, `docs/`, `.github/`, `AGENTS.md`, ...). Anything else, like your notes or `.vscode/`, is left alone and listed |
| `./lab apply N [--tools claude,codex]` | Copies step N's example in and commits **only those files** as `lab: stepNN <name>`. Refuses if there are uncommitted changes in the lab's folders, so a Before run can't end up inside the commit |
| `./lab goto N [--solutions]` | Catch up: installs every step before N, each as its own commit (plus reference solutions, where a step has one) |

To run a step's Before again after installing it, step back with `git reset --hard HEAD~1` (that
removes the `lab: stepNN` commit).

## Try

1. **Make it seven.** In [Index.cshtml.cs](../../src/Tasks.App/Pages/Index.cshtml.cs), change
   `tasks.Completed().Count` to `tasks.Completed().Count()`. The Problems panel picks it up within
   seconds; `./lab lint` says `Total: 7 (baseline 6)` and `./lab lint --check` exits 1. Undo it.
2. **Fix one.** Make `Describe` `static`. The total drops to 5 and `--check` passes. Should the baseline
   drop to 5 too? Step 7 says yes: that's the ratchet. Undo it, because Step 7 needs the debt.
3. **Turn a rule off.** Delete the CA1304 line from `src/.editorconfig`: the count drops without any
   code changing. That's why Step 7's guard protects the lint configuration as well as the baseline.
   Undo it.
4. **Ask again, differently.** In a new session: "Fix the problems in Recent.cshtml.cs." Does the agent
   run `./lab lint` (or check Problems) afterwards to prove they're gone? Usually not, unless something
   tells it to. That's Step 1.

## If it doesn't match

- **Problems panel shows nothing, even with `Recent.cshtml.cs` open:** the file was probably opened
  before C# Dev Kit finished loading the solution (30–50 s), so Roslyn treated it as a loose
  "file-based program" instead of part of `Tasks.App`. You can tell because the C# output log says
  `[FileBasedProgramsProjectSystem] Successfully completed load of .../Recent.cshtml.cs`, and the
  editor shows a dotted suggestion under `RecentModel` but no warnings. Fix: close all editors, run
  **Developer: Reload Window**, wait until the status bar shows `Projects: Tasks.App`, then open the
  file again. (Seen with VS Code Insiders, C# 2.160.4, C# Dev Kit 3.40.215.)
- **Problems panel shows a different number than `./lab lint`:** the two `fullSolution` settings in
  `.vscode/settings.json` are missing (by default only open files are checked), or the extension is
  still loading. Suggestions (dotted underlines) aren't counted; filter the panel to warnings.
- **Using VS Code Insiders?** It has its own extensions and settings: check the C# and C# Dev Kit
  extensions are installed there, not only in stable VS Code.
- **`./lab lint` shows a different number:** check `dotnet --version`. The rule set and level are
  pinned, so SDK 10 and 11 should agree; if they don't, that's spike S3 failing.
- **`./lab doctor` says NuGet failed:** the test packages couldn't restore. Check
  `dotnet nuget list source` and any proxy.
- **`./lab` isn't executable:** `chmod +x lab`.

## For us: what this step proves, and what's left

Verified on the Mac (SDK 10.0.102, macOS arm64, 17 Nov 2026):

- [x] `global.json` floor + `latestMajor`, `net10.0`: builds and tests (spike S1, first half)
- [x] Pinned rule set gives exactly six findings from `dotnet build` and `./lab lint` (spike S3, first half)
- [x] `./lab doctor` all green; `./lab lint --json` and `--check` behave
- [x] VS Code Problems panel shows the same six, after a reload (Steve, VS Code Insiders)
- [x] Before/After by hand in VS Code: the Before didn't find the lint findings, the After did (Steve)
- [ ] Before/After validated with the harness: strong model Before, small model After, 3 runs each
- [ ] Same count on SDK 11 and on Windows (spike S3, second half)
- [ ] An SDK 9 machine gets a readable refusal (spike S1, second half)
- [x] `./lab apply 0` / `reset` / `goto` / `list` replace the manual `cp` (tested in a throwaway clone)
- [x] Part C tried in VS Code: works as described, including Find All References through `ITaskQueries` (Steve, 17 Nov)
- [ ] Part C in the Copilot CLI via `.github/lsp.json` (spike S7)

Found while building it (goes into BUILDING-LABS later):

- **CA1822 doesn't fire on a method used only as a method group** (`Select(Describe)`). Called
  directly, it does.
- **IDE0059 (unused assignment) didn't appear in the command-line build** even with
  `EnforceCodeStyleInBuild`, so it's not used. IDE rules can show in the editor but not the build,
  which would break "Problems panel = `./lab lint`". The lab sticks to CA rules and compiler warnings.
- **Files opened during solution load can get stuck as "file-based programs"** in C# 2.160 (no
  diagnostics). The walkthrough says to wait.
- **No time-zone abbreviations in .NET.** `TimeZoneInfo` gives names like "New Zealand Standard Time",
  not `NZDT`. Step 1's zone-label rule needs restating, for example as the offset (`10:00 +13:00`), the
  zone ID, or a small lookup we ship. Decide before building Step 1.
- **Scoring item 5 needs the transcript.** The CLI harness can score it from `--share`; in VS Code it's
  by eye.
