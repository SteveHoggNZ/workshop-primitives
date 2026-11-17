# Step 1 walkthrough: project instructions (`AGENTS.md`) and a scoped rule

Every step is an experiment: run a prompt **without** the primitive, install it, run the **same**
prompt again in a new session, and compare. If you install first, there's nothing to compare against.

Start from step 0 installed: `./lab list` shows `[x] 00  lint`. If not, `./lab goto 1`.

1. **Before.** Start a new agent session (don't install anything yet) and give it this prompt:

   ```text
   Add a /Completed page, like the Recent page, that lists completed tasks and shows when the page was loaded.
   ```

   Score it with the checklist below. Don't paste the checklist into the prompt.
2. **Reset.** `./lab reset` discards the Before changes.
3. **Install.** `./lab apply 1` copies three files in and commits them as `lab: step01 instructions`.
   Add `--tools claude` for the Claude Code adapter.
4. **After.** Start a **new** session (old ones don't see new files), give it the same prompt, and score it again.
5. **Compare** the two scores, then read on.

> The rest of this page reveals the checklist answers. Read it after your Before run.

## Checklist (one point each)

| # | The result... |
|---|---|
| 1 | gets tasks through `TaskService` and the time from `TimeProvider` (no `TaskStore`, no `DateTime.Now`), the page works, and `dotnet test` passes |
| 2 | shows the load time in the user's zone with the UTC offset: `10:00 +13:00` in the tests, not `10:00:12` |
| 3 | lists `/Completed` in the Pages table in `docs/architecture.md` |
| 4 | adds a line under `## Unreleased` in `CHANGELOG.md` |
| 5 | quotes the `./lab lint` `Total:` line in the agent's final message |
| 6 | follows the page template conventions: `<section data-page="completed">`, rows `data-testid="task-row"` with `data-task-id`, and `<partial name="_Empty" />` for no tasks |

## Quick links

**The primitive**
- [AGENTS.md](../examples/step01-instructions/AGENTS.md): the rules, loaded in every session
- [.github/instructions/pages.instructions.md](../examples/step01-instructions/.github/instructions/pages.instructions.md): template conventions, loaded only when a `.cshtml` page is in play
- [.github/copilot-instructions.md](../examples/step01-instructions/.github/copilot-instructions.md): a one-line pointer to `AGENTS.md`
- [_other-tools/claude/CLAUDE.md](../examples/step01-instructions/_other-tools/claude/CLAUDE.md): Claude Code adapter (`--tools claude`). Codex reads `AGENTS.md` itself.

**Code the agent reads**
- [Pages/Recent.cshtml.cs](../../src/Tasks.App/Pages/Recent.cshtml.cs) and [Recent.cshtml](../../src/Tasks.App/Pages/Recent.cshtml): the page the prompt says to copy
- [Services/TaskService.cs](../../src/Tasks.App/Services/TaskService.cs): `TaskService`, including `Completed()`
- [Pages/Index.cshtml.cs](../../src/Tasks.App/Pages/Index.cshtml.cs): a page that does it right
- [AppSettings.cs](../../src/Tasks.App/AppSettings.cs) and [Program.cs](../../src/Tasks.App/Program.cs#L13-L17): the user's zone and the injected `TimeProvider`

<details>
<summary>Where to check the result (spoilers)</summary>

- New files: `src/Tasks.App/Pages/Completed.cshtml` and `Completed.cshtml.cs` (`git status`)
- [Pages/Shared/_Layout.cshtml](../../src/Tasks.App/Pages/Shared/_Layout.cshtml) (nav)
- [docs/architecture.md](../../docs/architecture.md) (Pages table)
- [CHANGELOG.md](../../CHANGELOG.md) (`## Unreleased`)
- The session transcript or final message (lint `Total:` line)

</details>

## What this step shows

The code teaches by example: a model that reads a few files learns "use `TaskService`, use the
injected clock" without being told. It can't learn team decisions nobody wrote down: how times are
shown, which doc lists pages, where the changelog goes, which command proves the work is clean, or a
template convention the older pages don't follow yet. `AGENTS.md` is where those decisions live, and a
scoped instructions file holds the ones that only matter for some files.

## What the code already teaches

An agent asked for "a page like Recent" reads `Recent.cshtml.cs`, then looks at how other code works.

- **One data path.** [`TaskService`](../../src/Tasks.App/Services/TaskService.cs#L6-L7) is "the only way
  the rest of the app reads or changes tasks", and it already has
  [`Completed()`](../../src/Tasks.App/Services/TaskService.cs#L36-L37).
- **Injected time.** [`Program.cs`](../../src/Tasks.App/Program.cs#L13-L17) registers a `TimeProvider`;
  tests pin it with `FixedTimeProvider`.
- **A page that does it right.** [`Index.cshtml.cs`](../../src/Tasks.App/Pages/Index.cshtml.cs#L7-L20)
  takes `TaskService` in its constructor.

The page the prompt points at is the bad example: it takes `TaskStore` directly, reads `DateTime.Now`,
prints `HH:mm:ss` with no zone, and throws on an empty list. A strong model usually sees past this and
uses the service and clock anyway, so most Befores pass item 1. The other five items are different.

## What the code can't teach

Nothing in `src/` shows any of these, so there's nothing to copy.

### The zone label (item 2)

- **Where it applies:** the load time. The zone is `AppSettings.Zone` (`APP_TZ`, `Pacific/Auckland`).
- **Typical Before:** the clock is injected, so the time is right, but it prints `10:00:00` or `10:00`
  with no offset, or uses the machine's zone instead of `AppSettings.Zone`.
- **Closing rule:** [user-facing times in the user's zone with the UTC offset](../examples/step01-instructions/AGENTS.md#L20-L21).
- **Why an offset, not `NZDT`:** .NET has no built-in zone abbreviations. The offset is exact and
  checkable by a script.

### The architecture table (item 3)

- **Where it applies:** the [Pages table](../../docs/architecture.md#L15-L19). Nothing in the code points at it.
- **Typical Before:** a nav link in `_Layout.cshtml`; the table untouched.
- **Closing rule:** [listed in the Pages table](../examples/step01-instructions/AGENTS.md#L28).
- **Watch for:** the doc has deliberate errors for Step 3 (SQLite, `/Reports`, `TaskCommands`, ...).
  An After that "tidies" them changes Step 3's result. Run `./lab reset` or `./lab goto 3` before Step 3.

### The changelog (item 4)

- **Where it applies:** the empty `## Unreleased` section in [CHANGELOG.md](../../CHANGELOG.md).
- **Typical Before:** untouched. Nothing in a small feature leads an agent to the changelog.
- **Closing rule:** [a line under `## Unreleased`](../examples/step01-instructions/AGENTS.md#L29).

### The lint count (item 5)

- **Where it applies:** `./lab lint` prints `Total: 6 (baseline 6)`, the same as the Problems panel (Step 0).
- **Typical Before:** `dotnet test` passes and the agent stops. Nothing asks for a lint report.
- **Why `./lab lint` and not `dotnet build`:** an incremental build prints no warnings for code that's
  already compiled, so "I built it, 0 warnings" can be false. `./lab lint` always rebuilds.
- **Closing rule:** [run `./lab lint` and quote its `Total:` line](../examples/step01-instructions/AGENTS.md#L30).

### The template conventions (item 6): the scoped rule

- **Where it applies:** the new `.cshtml`. The [`_Empty` partial](../../src/Tasks.App/Pages/Shared/_Empty.cshtml)
  exists, but no page uses it yet, and no page has `data-page` or `data-testid`.
- **Typical Before:** a plain `<ul>` copied from Recent, no empty state.
- **Closing rule:** [pages.instructions.md](../examples/step01-instructions/.github/instructions/pages.instructions.md),
  with `applyTo: "src/Tasks.App/Pages/**/*.cshtml"`. It costs nothing in sessions that don't touch a page.

## Reading the files

[AGENTS.md](../examples/step01-instructions/AGENTS.md) (36 lines):

- [Stack](../examples/step01-instructions/AGENTS.md#L3-L6): only what the agent would otherwise guess.
- [Layout](../examples/step01-instructions/AGENTS.md#L8-L14): a map, not a tour.
- [Rules](../examples/step01-instructions/AGENTS.md#L16-L23): the service and clock rules restate the
  code; the zone rule is new information; the last line points at the scoped file.
- [Definition of done](../examples/step01-instructions/AGENTS.md#L25-L30): where items 3–5 come from.
- [Commands](../examples/step01-instructions/AGENTS.md#L32-L36): the exact strings to run.

What to notice:

- **Short.** It's loaded in every session; every extra line dilutes the rest.
- **Only what applies to every task.** Template conventions live in the scoped file instead.
- **Each rule answers a failure.** If you can't name the mistake a rule prevents, cut it.
- **One canonical copy.** The Copilot and Claude files are pointers with no rules of their own.

## Did it load?

- **Strongest evidence:** the After's final message quotes the `Total:` line. Nothing else asks for it.
- **VS Code:** expand the response's references; `AGENTS.md` (or `copilot-instructions.md`) and, once
  the agent works on the `.cshtml`, `pages.instructions.md` should be listed.
- **Copilot CLI:** the session log shows the instruction files being read.

## Scoring: where to look

| Item | Where to check |
|---|---|
| 1 | `Completed.cshtml.cs` (no `TaskStore`, no `DateTime.Now`), `dotnet test`, then open `/Completed` |
| 2 | A test or the page: `10:00 +13:00` with the pinned clock (or the real time with `+13:00` when you run the app). In raw HTML Razor writes `+` as `&#x2B;`, so a test that searches the HTML must `WebUtility.HtmlDecode` it first; an agent's test that "fails mysteriously" here is usually this |
| 3 | `docs/architecture.md` Pages table |
| 4 | `CHANGELOG.md`, under `## Unreleased` |
| 5 | The final message or transcript. It leaves no trace in `git diff` |
| 6 | `Completed.cshtml`: `data-page="completed"`, `data-testid="task-row"` + `data-task-id`, `<partial name="_Empty" />` |

The reference solution is [.lab/solutions/step01.patch](../solutions/step01.patch)
(`./lab goto 2 --solutions` installs it).

## If the After didn't improve

- **Old session.** Sessions read instructions at the start; start a new one.
- **Not installed.** `./lab list` shows `[x] 01  instructions`, and `AGENTS.md` is at the repo root.
- **VS Code settings.** `chat.useAgentsMdFile` (for `AGENTS.md`) and
  `github.copilot.chat.codeGeneration.useInstructionFiles` (for `.github/copilot-instructions.md` and
  `.github/instructions/`) must be on.
- **Personal instructions.** User-level instructions or skills change both runs; turn them off.

## Try

1. Delete the [zone rule](../examples/step01-instructions/AGENTS.md#L20-L21) from the root `AGENTS.md`
   and run the prompt in a new session. Does the offset disappear? Then the rule is doing the work.
2. Which rules could a linter enforce instead? Banning `DateTime.Now`, or `TaskStore` outside
   `TaskService`, is a tool's job (Step 0 showed how cheap that is). Which ones need judgement? Step 7
   picks this up.
3. Add a line to `AGENTS.md`: "To find callers or references, use #usages, not text search." Re-run
   Step 0 Part C in a new session.

## For us

- [ ] Before/After by hand in VS Code (Steve)
- [x] **Validated with the harness** (Copilot CLI 1.0.112, 17 Nov 2026), 2 runs per arm, no flips:
  Before (gpt-6.1-sol) **1/6, 1/6**; After (gpt-5.6-luna) **6/6, 6/6**. Both Befores passed only item 1
  (the code teaches the service and clock) and missed every team decision. Scorer:
  `.lab/scripts/steps/step01.py` with hidden checks `.lab/scripts/checks/LabCheck01.cs`.
- [x] Scorer calibrated: reference solution 6/6, untouched clone 0/6
- [x] Reference solution passes the tests and leaves lint at 6; `goto 2 --solutions` applies it
