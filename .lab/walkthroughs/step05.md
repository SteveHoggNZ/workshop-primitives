# Step 5 walkthrough: a diagnostics skill (measure first)

Every step is an experiment: run a prompt **without** the primitive, install it, run the **same**
prompt again in a new session, and compare. If you install first, there's nothing to compare against.

**Before you start:** Steps 0 to 4 must be installed, and `./lab warm` must have run once on this
machine (it downloads the profiler's packages; see Timing). Check with:

```bash
./lab list              # Windows: .\lab list
```

You should see an `x` next to steps 00 to 04. If not, run `./lab goto 5`. Then open `/Bulk`
(`dotnet run --project src/Tasks.App`), click **Generate 10,000 sample tasks**, and search for
`sample task 12`: each search takes about a second (more under `dotnet run` on a busy machine: the Befores measured
about 3 s). **Stop the app (Ctrl+C) before the Before run**: on Windows a running app locks its DLL, and the agent's
build fails.

1. **Before.** Start a new agent session (don't install anything yet) and give it this prompt:

   ```text
   Search on /Bulk is very slow once you click 'Generate 10,000 sample tasks': each search takes about a second. Make it fast, and tell me how much faster it is.
   ```

   Score it with the checklist below. Don't paste the checklist into the prompt.
2. **Reset.** `./lab reset` discards the Before changes.
3. **Install.** `./lab apply 5` copies the skill in and commits it as `lab: step05 diagnostics`.
4. **After.** Start a **new** session, give it the same prompt, and score it again.
5. **Compare** the two scores, then read on.

## Checklist (one point each)

| # | The result... |
|---|---|
| 1 | is fast (at least 5× faster than before), finds the same tasks, and `dotnet test` passes |
| 2 | timed the search before changing `src/`, and again after |
| 3 | **profiled** before changing `src/`: measured where the time goes, not just how long it takes |
| 4 | says in the final message where the time went: a method and its measured share (for example "99.8% in ...") |

> The rest of this page explains what to look for. Read it after your Before run.

## Quick links

**The primitive**
- [.agents/skills/dotnet-diagnostics/SKILL.md](../examples/step05-diagnostics/.agents/skills/dotnet-diagnostics/SKILL.md): measure, fix, measure again, report
- [scripts/profile.cs](../examples/step05-diagnostics/.agents/skills/dotnet-diagnostics/scripts/profile.cs): the in-process profiler the skill runs

**Code the agent reads**
- [Pages/Bulk.cshtml.cs](../../src/Tasks.App/Pages/Bulk.cshtml.cs): generate and search
- [Services/SearchService.cs](../../src/Tasks.App/Services/SearchService.cs) and [Services/TextHelpers.cs](../../src/Tasks.App/Services/TextHelpers.cs)

<details>
<summary>Where to check the result (spoilers)</summary>

- `src/Tasks.App/Services/TextHelpers.cs`: the fix
- The transcript: a profile (or any measurement of *where*) before the first edit to `src/`
- The final message: before and after numbers, and where the time went

</details>

## What this step shows

A performance fix needs two numbers and a location: how slow it was, how fast it is now, and where
the time went. A strong model often profiles by habit. A small, cheap one reads the code, guesses
the hot spot, and times its own fix if asked. A **skill with a script** gives the small model the
habit and the tool: one command that says where the time goes, run the same way before and after.

What happened when we ran it (Copilot CLI 1.0.112, `gpt-5.6-luna` in both arms, 3 runs each):

| Item | Before | After (with the skill) |
|---|---|---|
| 1 fast, same results, tests pass | ✓ ✓ ✓ | ✓ ✓ ✓ |
| 2 timed before and after | ✓ ✓ ✓ | ✓ ✓ ✓ |
| 3 profiled before the fix | · · · | ✓ ✓ ✓ |
| 4 says where the time went | · · · | ✓ ✓ ✓ |
| **Score** | **2/4, 2/4, 2/4** | **4/4, 4/4, 4/4** |

The bug is visible to a careful reader: 120 `Regex` objects per title. Every Before found it by
reading, started the app, and timed `curl` before and after. That's a real measurement, and it's why
item 2 is a baseline. But one Before timed the wrong request (2.8 ms, not the slow search) and
reported "15% faster" for a 30× fix. Every After ran `profile.cs` first, saw
`99.9%  Tasks.App.Services.TextHelpers.Normalise`, fixed that method, ran the same command again, and
pasted both. Sessions: Before about 2 min (starting a server, polling it), After about 3–3.4 min: slower by
the clock (each profile run is 10–20 s), cheaper in credits.

## What the code already teaches

- **Where to look.** The page calls `SearchService.Count`, which calls `TextHelpers.Normalise` for
  every title. A short read. On a slow path this obvious, reading often works.
- **How to time it.** "Tell me how much faster it is" asks for a measurement, and an agent can time
  a request with `curl` or a test. Items 1 and 2 are baselines.

## What the code can't teach

### Measure where, before fixing (item 3): the script

- **Where it applies:** any performance problem, and especially the ones where the obvious suspect
  isn't the cause. Reading finds a plausible cause; a profile finds the actual one.
- **Closing rule:** [the Measure section](../examples/step05-diagnostics/.agents/skills/dotnet-diagnostics/SKILL.md#L12-L27)
  and [profile.cs](../examples/step05-diagnostics/.agents/skills/dotnet-diagnostics/scripts/profile.cs). It hosts
  the app in-process (`WebApplicationFactory`, like the tests), runs the setup request, times the page,
  and **samples the CPU** with EventPipe, the runtime's own tracing, by opening a session on itself.
  No server, no port, no `dotnet-trace` install (often blocked on corporate machines).
- **Read the "own time" list.** The deepest app method on the stack in each sample. In our runs 99.9% of
  samples were in `TextHelpers.Normalise`. The shares are of samples in app code, so after the fix the
  same method can still be on top with fewer samples: compare the latency.

### Say where the time went (item 4): the report rule

- **Closing rule:** [Report](../examples/step05-diagnostics/.agents/skills/dotnet-diagnostics/SKILL.md#L38-L42):
  paste both `latency ms:` lines and the top "own time" line from each run, unchanged. As in Steps 2
  and 4, it says why: the reader should see the measurement, not take your word for it.

## The cheaper-model point

A strong model may profile on its own: the Python version of this lab found a large model did,
every time. The skill is how a small, cheaper model does the same job, with numbers you can check.
Look at the "AI Credits" line the Copilot CLI prints at the end of each session: here the Afters
used about 1.0 credits and the Befores about 1.6 (starting a server and polling it with `curl` takes
more turns than one script). Same model, less spent, and the answer has the evidence in it.

## Reading the files

[SKILL.md](../examples/step05-diagnostics/.agents/skills/dotnet-diagnostics/SKILL.md) (42 lines):

- [The description](../examples/step05-diagnostics/.agents/skills/dotnet-diagnostics/SKILL.md#L3): use it when
  something is slow, laggy or CPU-heavy, "or when asked how much faster a change made it". **Not** for
  wrong output, errors or crashes: that's the `tdd` skill. Two skills now share the repo, and each
  description says which work belongs to the other.
- The rule first, with its reason: "a fix in the wrong place doesn't move the numbers".
- Measure, Fix, Measure again, Report.

[profile.cs](../examples/step05-diagnostics/.agents/skills/dotnet-diagnostics/scripts/profile.cs)
(about 180 lines): a .NET 10 file-based app (`dotnet run file.cs`). `#:project` references the app;
`#:package` brings `Microsoft.Diagnostics.NETCore.Client` (to start an EventPipe session on its own
process) and `Microsoft.Diagnostics.Tracing.TraceEvent` (to read the samples and their stacks).

## Did it load?

- **Copilot CLI:** a `skill` call for `dotnet-diagnostics`, then `dotnet run --no-cache .agents/skills/dotnet-diagnostics/scripts/profile.cs ...`.
- **VS Code:** the response's references list `SKILL.md`; the terminal shows `profile.cs` twice.
- **Did the tdd skill load instead?** It shouldn't: its description says it's not for slowness.

## Scoring: where to look

| Item | Where to check |
|---|---|
| 1 | `/Bulk` after generating: a search returns at once and the same count as before; `dotnet test` passes |
| 2 | The transcript: a timing run (`profile.cs`, `curl -w`, `time`, a stopwatch) before the first edit to `src/` and another after the last |
| 3 | The transcript: a profiler run (`profile.cs`, `dotnet-trace`, `dotnet-counters`) before the first edit to `src/` |
| 4 | The final message: a method name with a percentage that also appears in a profiler's output |

The reference solution is [.lab/solutions/step05.patch](../solutions/step05.patch)
(`./lab goto 6 --solutions` installs it): two `[GeneratedRegex]` patterns built once, 1,040 ms → 33 ms.

## Timing

- `./lab warm` (once, in setup) builds the profiler script and downloads its two packages. Without
  it, the first `profile.cs` run downloads them during the step.
- `profile.cs` takes **6–10 s while the page is slow** on a quiet machine (it rebuilds, then times the
  1 s page four times; we measured 9.7 s for the first run in a fresh copy and 6.3 s after that), and
  13–21 s when the machine is busy (as it was for part of our validation). Under 10 s once the page is fast.

## If the After didn't improve

- **Old session, not installed, didn't load:** as in Steps 2 and 4.
- **`profile.cs` failed to build:** run `./lab warm`; if NuGet can't reach the two packages (a corporate
  feed), the walkthrough's fallback is to time with `curl` and read the code, which is the Before.
- **The tdd skill loaded.** Check the two descriptions: each should say the other's work isn't its own.

## Try

1. **Profile something that isn't slow.** `--path "/Dashboard"`: no hot method stands out. A profile
   that shows nothing is also an answer.
2. **Break the routing.** Delete "Not for slowness, CPU or memory problems" from the tdd skill's
   description, start a new session, and give the Step 5 prompt again. Which skill loads?

## For us

- [ ] Before/After by hand in VS Code (Steve), plus the VS Code check below
- [x] **Validated with the harness** (Copilot CLI 1.0.112, 17 Nov 2026), `gpt-5.6-luna` in **both** arms,
  3 runs each, no flips: Before **2/4, 2/4, 2/4**; After **4/4, 4/4, 4/4** (checklist v2, skill v3). Every
  After loaded the skill and profiled first. Checklist v1's pilot Before timed with curl and paraphrased the
  numbers, so v2 made timing a baseline and scores where the time went; one scorer fix (curl `-w` timing
  wasn't recognised). Details in `.lab/validation/NOTES.md`. Scorer: `.lab/scripts/steps/step05.py`, hidden
  checks `.lab/scripts/checks/LabCheck05.cs` (timed against a copy of the original code in the same process).
- [x] Scorer calibrated: reference solution 4/4, untouched clone 0/4
- [ ] 5b (memory growth) not built
- [ ] Windows: `profile.cs` (EventPipe self-session) is untested on Windows

### VS Code check for Steve (about 3 minutes)

1. `./lab goto 6` in a scratch branch (or `./lab apply 5`), `./lab warm`, then reload the window.
2. In the terminal, run the skill's command once yourself (copy it from SKILL.md) and check it prints
   `latency ms:` and `TextHelpers.Normalise` at the top of "own time".
3. In Chat (agent mode), send the Step 5 prompt. Expected: references list `SKILL.md`, and the
   terminal runs `profile.cs` before any edit to `TextHelpers.cs`.
