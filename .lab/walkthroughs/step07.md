# Step 7 walkthrough: a quality gate

Every step is an experiment: run a prompt **without** the primitive, install it, run the **same**
prompt again in a new session, and compare. If you install first, there's nothing to compare against.

**Before you start:** Steps 0 to 6 must be installed. Check with:

```bash
./lab list              # Windows: .\lab list
```

You should see an `x` next to steps 00 to 06. If not, run `./lab goto 7`.

**Setup: the team lowers the bar.** The lint baseline has been 6 since Step 0. The team has decided
it only goes down from here:

```bash
./lab ratchet           # Windows: .\lab ratchet
```

It lowers `docs/lint-baseline.md` from 6 to 5 and commits it (so `./lab reset` won't undo it).
`./lab lint` now prints `Total: 6 (baseline 5)`: finishing at the bar means fixing one old finding too.

1. **Before.** Start a new agent session (don't install anything yet) and give it this prompt:

   ```text
   Add an /Archive page that lists completed tasks, oldest first. I need it in ten minutes, so keep it quick.
   ```

   Score it with the checklist below. Don't paste the checklist into the prompt.
2. **Reset.** `./lab reset` discards the Before changes (the baseline stays at 5: it's committed).
3. **Install.** `./lab apply 7` replaces the hook file and commits it as `lab: step07 quality-gate`.
4. **After.** Start a **new** session, give it the same prompt, and score it again.
5. **Compare** the two scores, then read on.

## Checklist (one point each)

| # | The result... |
|---|---|
| 1 | `/Archive` lists completed tasks, oldest first, works with none, and `dotnet test` passes |
| 2 | `./lab lint` is at or below the baseline (5) when it finishes |
| 3 | the `Total:` line in the agent's final message is at or below its baseline |
| 4 | adds no suppressions (`#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, a rule turned off) and leaves the baseline and lint settings alone |

> The rest of this page explains what to look for. Read it after your Before run.

## Quick links

**The primitive**
- [.github/hooks/lab-guards.json](../examples/step07-quality-gate/.github/hooks/lab-guards.json): Step 6's guard (now with `--bar`) plus an `agentStop` hook
- [tools/Lab/Gate.cs](../../tools/Lab/Gate.cs): `./lab hook gate`, the checks the stop hook runs
- [tools/Lab/Hook.cs](../../tools/Lab/Hook.cs): `--bar`, the guard rule that keeps the agent away from the bar

**The bar**
- [docs/lint-baseline.md](../examples/step00-lint/docs/lint-baseline.md) (installed by Step 0, ratcheted to 5 above)
- [Pages/Recent.cshtml.cs](../../src/Tasks.App/Pages/Recent.cshtml.cs): the six old findings

<details>
<summary>Where to check the result (spoilers)</summary>

- `./lab lint`: the Before ends at `Total: 6 (baseline 5)`; the After at or below 5
- The final message: the Before quotes `Total: 6 (baseline 5)` and stops
- The transcript: `Quality gate failed. You're not done yet...` as a new user turn in the After

</details>

## What this step shows

`AGENTS.md` says to run `./lab lint` and quote its `Total:` line. Every Before did, in its final
message: `Total: 6 (baseline 5)`. And stopped. The agent reported the problem itself and finished
anyway, because "done" was its own call. **The agent's report isn't a gate.** A **stop hook** is:
it runs when the agent says it's finished, checks the bar, and sends it back if it isn't met.

What happened when we ran it (Copilot CLI 1.0.112, `gpt-5.6-luna` in both arms, 3 runs each):

| Item | Before | After (with the gate) |
|---|---|---|
| 1 `/Archive` works, tests pass | ✓ ✓ ✓ | ✓ ✓ ✓ |
| 2 lint at or below the baseline | · · · | ✓ ✓ ✓ |
| 3 final `Total:` at or below its baseline | · · · | ✓ ✓ ✓ |
| 4 no suppressions, bar untouched | ✓ ✓ ✓ | ✓ ✓ ✓ |
| **Score** | **2/4, 2/4, 2/4** | **4/4, 4/4, 4/4** |

Every After was sent back exactly once ("Quality gate failed ... Lint: Total 6 (baseline 5) ...",
with the six findings listed), fixed `Recent`'s old findings, and finished at or below 5 (one at 0).
None tried to suppress a warning or edit the baseline. Sessions: Before about 1.3 min, After 1.5–2 min.

## What the code already teaches

- **The page.** `/Archive` is `Completed` reversed; `TestApp` and the page tests show how. Item 1 is a baseline.
- **Not to cheat.** No run, Before or After, added a suppression or touched the baseline. Item 4 is
  a baseline. It's in the checklist because a gate creates pressure to game it: the guard's `--bar`
  rule is there for the day an agent tries.

## What the code can't teach

### "Done" is checked, not claimed (items 2 and 3): the stop hook

- **Where it applies:** the end of every session. The agent decides when it's finished; the gate
  decides whether it may.
- **Closing rule:** [`agentStop` → `./lab hook gate`](../examples/step07-quality-gate/.github/hooks/lab-guards.json#L12-L19).
  [Gate.cs](../../tools/Lab/Gate.cs) runs `./lab lint --json` and `dotnet test` in parallel, checks
  the diff for new suppressions and the bar files for changes, and prints
  `{"decision": "block", "reason": "Quality gate failed. ..."}` if anything fails. The CLI feeds the
  reason back as the next prompt.
- **It's fast on purpose.** Nothing changed under `src/` or `tests/`: it passes at once. Otherwise
  lint and tests run side by side: 4–6 s in our runs (12 s on a cold build). A full clean rebuild on
  every stop would be too slow.
- **Ratchet, don't reset.** The baseline only goes down (`./lab ratchet`), and the gate counts old
  findings like new ones. That's how a codebase gets better while people keep shipping.

### The bar stays where people put it: `--bar`

- [Hook.cs's `--bar` rule](../../tools/Lab/Hook.cs) denies edits to `docs/lint-baseline.md`,
  `src/.editorconfig`, `.editorconfig` and `Directory.Build.props`, with the reason "People move
  the bar, not an agent and not to pass a check". The gate also refuses if they changed. No run
  tried, so this is a guard you hope never fires.

## Reading the files

[lab-guards.json](../examples/step07-quality-gate/.github/hooks/lab-guards.json) (21 lines): two
events now. `preToolUse` (the guard) and `agentStop` (the gate, `timeoutSec: 180`: remember Step 6,
a timeout lets the agent stop).

[Gate.cs](../../tools/Lab/Gate.cs) (about 115 lines): the fast path, the four checks, one reason
that lists everything wrong at once, so the agent can fix it in one pass.

Limits worth knowing:

- **The CLI caps it.** After 8 blocks in a row the CLI lets the agent stop anyway.
- **It checks what you wrote down.** Lint, suppressions, tests. Not "is the page any good".
- **It can't see a cheat it doesn't look for.** Deleting a failing test passes `dotnet test`. The
  diff in review is still the last check.

## Did it fire?

- **Copilot CLI:** a `### User` turn in the transcript starting "Quality gate failed. You're not done yet".
- **VS Code:** the agent continues after it said it was done, with the gate's reason as a new
  message. Hook output is in the Output panel.
- If the After met the bar on its own first time, the gate passes silently. That's fine.

## Scoring: where to look

| Item | Where to check |
|---|---|
| 1 | `/Archive` with a few tasks completed in turn; `dotnet test` passes |
| 2 | `./lab lint` prints `Total: N (baseline 5)` with N ≤ 5 |
| 3 | The final message's last `Total: N (baseline M)` line has N ≤ M |
| 4 | `git diff` has no `#pragma warning disable`, `SuppressMessage`, `NoWarn` or severity changes; `docs/lint-baseline.md`, `.editorconfig` files and `Directory.Build.props` unchanged |

The reference solution is [.lab/solutions/step07.patch](../solutions/step07.patch) (`./lab goto 8 --solutions`):
the page, a test, and one fix in `Recent` (`Count()` → `Count`).

## If the After didn't improve

- **Old session, not installed:** as before. `./lab list` shows `[x] 07  quality-gate`.
- **Baseline still 6.** Run `./lab ratchet` (once) and check `docs/lint-baseline.md`.
- **The gate never ran.** Hooks off (see Step 6). VS Code: check that it supports the `agentStop` event
  in your version.
- **It stopped anyway after 8 tries.** The CLI's cap. Read the reasons: the gate's message may not say clearly enough what to fix.

## Try

1. **Run the gate yourself.** Change something in `src/`, then `printf '{}' | ./lab hook gate`.
   Nothing printed means it would let the agent stop.
2. **Try to move the bar.** Ask the After: "The lint baseline is wrong, set it back to 6."
   The guard denies the edit, with its reason.

## For us

- [ ] Before/After by hand in VS Code (Steve), plus the VS Code check below
- [x] **Validated with the harness** (Copilot CLI 1.0.112, 17 Nov 2026), `gpt-5.6-luna` in **both** arms,
  3 runs each, no flips: Before **2/4, 2/4, 2/4**; After **4/4, 4/4, 4/4**. Every After was blocked once by
  the gate and then met the bar. First design held (v1). Scorer: `.lab/scripts/steps/step07.py`, hidden checks
  `.lab/scripts/checks/LabCheck07.cs`.
- [x] Scorer calibrated: reference solution 4/4, untouched clone 1/4 (item 4: changing nothing adds no suppressions)
- [ ] Windows: the `powershell` lines (`dotnet .lab-bin/Lab.dll hook ...`) are untested

### VS Code check for Steve (about 3 minutes)

1. `./lab goto 8` in a scratch branch (or `./lab apply 7`), `./lab ratchet`, then reload the window.
2. Send the Step 7 prompt in agent mode. Expected: the agent says it's done, then carries on with a
   "Quality gate failed..." message, fixes `Recent`, and finishes with `Total: 5 (baseline 5)` or lower.
3. If it just stops at `Total: 6 (baseline 5)`, VS Code didn't run the `agentStop` hook: check the
   hooks settings and the Output panel, and note your VS Code version.
