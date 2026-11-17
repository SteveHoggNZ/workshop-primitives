# Step 4 walkthrough: a TDD skill with phase state

Every step is an experiment: run a prompt **without** the primitive, install it, run the **same**
prompt again in a new session, and compare. If you install first, there's nothing to compare against.

**Before you start:** Steps 0 to 3 must be installed. Check with:

```bash
./lab list              # Windows: .\lab list
```

You should see an `x` next to steps 00 to 03:

```text
[x] 00  lint
[x] 01  instructions
[x] 02  skill
[x] 03  docs-reviewer
[ ] 04  tdd-skill
```

If not, run `./lab goto 4`. Then open `/Dashboard` once (`dotnet run --project src/Tasks.App`) to see
the page the bugs are on.

1. **Before.** Start a new agent session (don't install anything yet) and give it this prompt:

   ```text
   The dashboard has two bugs. 1) /Dashboard returns a 500 error when there are no tasks; it should show 0%. 2) On a Monday morning in Auckland (for example 2026-11-16 08:00 +13:00) 'Week starting' shows the previous Monday (9 Nov) instead of 16 Nov. Fix both.
   ```

   Score it with the checklist below. Don't paste the checklist into the prompt.
2. **Reset.** `./lab reset` discards the Before changes.
3. **Install.** `./lab apply 4` copies the skill in and commits it as `lab: step04 tdd-skill`.
4. **After.** Start a **new** session, give it the same prompt, and score it again.
5. **Compare** the two scores, then read on.

## Checklist (one point each)

| # | The result... |
|---|---|
| 1 | fixes both bugs (`/Dashboard` shows 0% when empty; "Week starting: 16 Nov" on Monday morning), and `dotnet test` passes |
| 2 | adds tests for both bugs |
| 3 | ran a test and saw it **fail** before changing anything in `src/` |
| 4 | used `./lab tdd`: a `GREEN:` line (a real failure recorded) and a `DONE:` line (the suite passes) |
| 5 | pastes, in the final message, the failure each test caught |

> The rest of this page explains what to look for. Read it after your Before run.

## Quick links

**The primitive**
- [.agents/skills/tdd/SKILL.md](../examples/step04-tdd-skill/.agents/skills/tdd/SKILL.md): the team's test-first procedure
- [tools/Lab/Tdd.cs](../../tools/Lab/Tdd.cs): `./lab tdd red|green|done`, the phase checks the skill runs

**Code the agent reads**
- [Services/TaskStats.cs](../../src/Tasks.App/Services/TaskStats.cs): the percentage and the week start (both bugs)
- [Pages/Dashboard.cshtml.cs](../../src/Tasks.App/Pages/Dashboard.cshtml.cs): the page
- [tests/Tasks.App.Tests/DashboardTests.cs](../../tests/Tasks.App.Tests/DashboardTests.cs) and [TestApp.cs](../../tests/Tasks.App.Tests/TestApp.cs): how pages are tested, with the clock pinned

<details>
<summary>Where to check the result (spoilers)</summary>

- `src/Tasks.App/Services/TaskStats.cs` and `tests/Tasks.App.Tests/DashboardTests.cs`
- The session transcript: the order of the test run and the first edit to `src/`, and the `./lab tdd` lines
- The final message (the pasted failures)

</details>

## What this step shows

Asked to fix a bug, an agent fixes it, adds a test, runs the suite and reports "tests pass". All
true, and none of it shows the test would have caught the bug: it was never seen failing. A
**skill** can carry the team's rule (red first), and a **command with phase state** makes the rule
checkable: `./lab tdd green` only moves on if a test really fails, and `./lab tdd done` only finishes
when it passes. The red becomes evidence in the transcript, not a claim in the final message.

What happened when we ran it (Copilot CLI 1.0.112, `gpt-5.6-luna` in both arms, 3 runs each):

| Item | Before | After (with the skill) |
|---|---|---|
| 1 both bugs fixed, suite passes | ✓ ✓ ✓ | ✓ ✓ ✓ |
| 2 tests for both bugs | ✓ ✓ ✓ | ✓ ✓ ✓ |
| 3 a test failed before `src/` changed | · · · | ✓ ✓ ✓ |
| 4 `./lab tdd` GREEN and DONE | · · · | ✓ ✓ ✓ |
| 5 the failures pasted | · · · | ✓ ✓ ✓ |
| **Score** | **2/5, 2/5, 2/5** | **5/5, 5/5, 5/5** |

Both arms fix both bugs and add good tests: the prompt gives the exact time to test, and `AGENTS.md`
asks for a test. Every Before wrote the fix and the tests in **one** patch, then ran the suite once,
green. Every After did two rounds, one per bug: `red`, a test, `green` (which ran it and saw it fail),
the fix, `done`. Sessions: Before 1–1.5 min, After about 3.3 min (each round runs the suite twice).

## What the code already teaches

- **How to test a page at a fixed time.** `TestApp` pins the clock and has `app.Clock.Set(...)`;
  [`DashboardTests`](../../tests/Tasks.App.Tests/DashboardTests.cs) shows a page test. So item 2 is easy.
- **Where the bugs are.** `done * 100 / total` and `now.UtcDateTime.Date` in
  [`TaskStats`](../../src/Tasks.App/Services/TaskStats.cs) are a short read from the page model. Item 1 is a baseline.

## What the code can't teach

### Red first (items 3 and 4): the skill and `./lab tdd`

- **Where it applies:** every bug fix. A test written after the fix passes the first time you run
  it, whether or not it tests the bug. Only a run *before* the fix shows it can fail.
- **Closing rule:** [the skill's Red and Green sections](../examples/step04-tdd-skill/.agents/skills/tdd/SKILL.md#L14-L29):
  one behaviour, one test, `./lab tdd green`, then the smallest fix, then `./lab tdd done`.
- **Why a command, not just the rule.** "Write the test first" is a habit you can claim. `./lab tdd
  green` runs the suite and records the failing test names in `.tdd-phase`. It only counts a test in a
  new or changed test file (an old failing test isn't evidence), and refuses if `src/` changed since
  `red`. `done` refuses if the red test is gone, so deleting it doesn't count.
- **What it can't see.** It doesn't check that you left the failing test's assertion alone in green.
  The skill says not to; a reviewer reading the diff is still the check for that.
- **Not a compile error.** In C# the easy fake red is a test that calls a method that doesn't exist
  yet: it "fails", but only because it doesn't build. `green` reads the test results file (TRX):
  no results means the build failed, and it refuses ("the tests don't build, so nothing failed when it
  ran"). Try it (Try 1).

### Show the failure (item 5): the skill's final-message rule

- **Closing rule:** [Final message](../examples/step04-tdd-skill/.agents/skills/tdd/SKILL.md#L34-L37):
  paste the `FAIL` line `green` printed, unchanged. As in Step 2, it says why (the reviewer wants to
  see the failure, not "tests pass"), because small models trim final messages. The After pasted, for example:

  ```text
  FAIL Tasks.App.Tests.DashboardTests.Dashboard_week_starting_uses_the_local_monday: Assert.Contains() Failure: Sub-string not found ... Not found: "Week starting: 16 Nov"
  ```

## Reading the files

[SKILL.md](../examples/step04-tdd-skill/.agents/skills/tdd/SKILL.md) (37 lines):

- [The description](../examples/step04-tdd-skill/.agents/skills/tdd/SKILL.md#L3): when to use it
  (wrong output, an error page, an exception, a wrong date) and when **not** to: slowness, CPU and
  memory problems. Step 5 depends on that sentence: a performance task must not load this skill.
- The rule and its reason first ("a test written after the fix may never have caught the bug").
- Red, Green, Next, Final message: each step names the command.

[Tdd.cs](../../tools/Lab/Tdd.cs): about 185 lines of C#. `red` writes `.tdd-phase`; `green` runs
`dotnet test` with a TRX logger and checks for failures; `done` runs it again and checks the red tests
still exist and pass. It's in `tools/Lab` with the other lab commands, so it runs the same on Windows
(`lab tdd`), and later steps' hooks can read `.tdd-phase`.

## Did it load?

- **Copilot CLI:** the transcript starts with a `skill` call for `tdd`, then `./lab tdd red ...`.
- **VS Code:** the response's references list `SKILL.md`, and the terminal shows `./lab tdd` commands.
- **Strongest evidence:** `GREEN: 1 failing, for the right reason:` in the transcript, before the
  first change to `src/`.

## Scoring: where to look

| Item | Where to check |
|---|---|
| 1 | `/Dashboard` with no tasks shows `0%`; with `APP_NOW=2026-11-16T08:00:00+13:00` (PowerShell: `$env:APP_NOW='2026-11-16T08:00:00+13:00'`), "Week starting: 16 Nov"; `dotnet test` passes |
| 2 | `tests/` diff: a test for the empty dashboard and one for the week start |
| 3 | The transcript: a `dotnet test` or `./lab tdd green` run with a failing test, **before** the first edit to `src/`. One red before the first fix is enough for the point: the checklist doesn't ask for one per bug |
| 4 | The transcript: `GREEN:` and `DONE:` lines from `./lab tdd` |
| 5 | The final message: a `FAIL` (or `Assert.... Failure`) line in a code block |

Items 3–5 leave no trace in `git diff`. The wizard reads them from the session (see `.lab/skills/lab-wizard-session`).

The reference solution is [.lab/solutions/step04.patch](../solutions/step04.patch)
(`./lab goto 5 --solutions` installs it).

## If the After didn't improve

- **Old session.** Skills are discovered at session start; start a new one.
- **Not installed.** `./lab list` shows `[x] 04  tdd-skill`, and `.agents/skills/tdd/SKILL.md` exists.
- **The skill didn't load.** Check the transcript (see "Did it load?"). If it never loaded, the
  description didn't match the prompt.
- **A phase left over.** `./lab tdd status` says `Phase: red` from an earlier run: `./lab reset`
  removes `.tdd-phase`.

## Try

1. **The refusal you want.** `./lab tdd red "x"`, then `./lab tdd green` with no new test:
   "Refused: no test fails". Then add a test that calls a method that doesn't exist:
   "Refused: the tests don't build". Then a real failing assertion: `GREEN`.
2. **Delete the red test.** After `green`, delete the failing test and run `./lab tdd done`:
   "Refused: the test that failed in red is gone".
3. **When not to.** Ask a new session: "The dashboard feels slow. Make it faster." Does the skill
   load? It shouldn't: the description says it's not for slowness.

## Timing

`./lab tdd green` and `done` each run `dotnet test`: 10–20 s (it builds what changed, then runs the
suite; we measured 13–14 s for the test run alone on a busy machine). Each round of the After runs it twice.

## For us

- [ ] Before/After by hand in VS Code (Steve), plus the VS Code check below
- [x] **Validated with the harness** (Copilot CLI 1.0.112, 17 Nov 2026), `gpt-5.6-luna` in **both** arms,
  3 runs each, no flips: Before **2/5, 2/5, 2/5**; After **5/5, 5/5, 5/5**. Every After loaded the skill
  and did two red/green/done rounds. First design held (v1); one scorer fix (output read through
  `read_bash`), all runs re-scored. Details in `.lab/validation/NOTES.md`. Scorer:
  `.lab/scripts/steps/step04.py` with hidden checks `.lab/scripts/checks/LabCheck04.cs`.
- [x] Scorer calibrated: reference solution 5/5, untouched clone 0/5
- [x] Reference solution passes the tests and leaves lint at 6; `goto 5 --solutions` applies it
- [ ] Claude Code adapter: not built (Claude Code reads skills from `.claude/skills/`)

### VS Code check for Steve (about 3 minutes)

1. `./lab goto 5` in a scratch branch (or `./lab apply 4`), then reload the window.
2. In Chat (default agent, agent mode), send the Step 4 prompt. Expected: the response references
   `SKILL.md`, and the terminal shows `./lab tdd red ...`, then `./lab tdd green` with a `FAIL` line, before any
   edit to `src/Tasks.App/Services/TaskStats.cs`.
3. If VS Code asks to approve each terminal command, approve them: the point is the order of the
   commands, not the approvals. Note how long `./lab tdd green` takes on your machine.
