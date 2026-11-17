# Validation notes

Results live in `results.jsonl` (one line per scored run); `python3 .lab/scripts/lab.py results` prints them.
Every run that isn't counted is listed here with the reason. Never drop a run without a line here.

## Setup

- Copilot CLI 1.0.112 on macOS (arm64), .NET SDK 10.0.102, 17 Nov 2026.
- Before on `gpt-6.1-sol` (strong), After on `gpt-5.6-luna` (small). 2 runs per arm (decided 17 Nov).
- Clones in `../.lab-runs/`, built with `./lab goto`, `.lab/` removed; `COPILOT_HOME` isolated in
  `../.lab-homes/<model>`; `--allow-all --trust-folder --share`.

- **From Step 3 on (17–18 Nov overnight): `gpt-5.6-luna` in both arms**, Before and After (and every arm of Step 8).
  Copilot CLI 1.0.112, .NET SDK 10.0.102. Steps 1–2 were validated with a `gpt-6.1-sol` Before; same model in
  both arms from Step 3 keeps it to one variable: the primitive. At least 3 runs per arm.
- Runs go through `lab.py run` (adds `--no-ask-user` and `--usage-output-file`; one fresh session per turn).

## Harness changes

- **One build per score** (17 Nov): `score` copies the step's hidden checks in, runs `dotnet test` once and reads the
  visible tests and the `LabCheckNN` results from the same TRX, filtered by class name. If the build fails with the
  checks in, it runs once more without them so the visible tests are still judged on their own (as before). Checked
  on four calibration clones scored with the old and new code: identical (s01 ref 6/6, base 0/6; s02 ref 6/6,
  base 0/6). Warm re-score dropped to about 5 s a clone.
- `calibrate STEP` (reference patch + stub transcript from the scorer's `CALIB`, and an untouched clone, in
  parallel), `run` (Copilot CLI with timing and usage), `metrics`, and parallel `prepare`/`score`.

## Counted

| Step | Before | After | Verdict |
|---|---|---|---|
| 1 | 1/6, 1/6 | 6/6, 6/6 | Validated. No flips |
| 2 | 3/6, 3/6 (`v2r1`, `v2r2`) | 6/6, 6/6 (`v3r1`, `v3r2`) | Validated. Befores pass 1, 2, 4. Afters loaded the skill and ran `try.cs` |
| 4 | 2/5, 2/5, 2/5 (`v1-r1..r3`, Luna) | 5/5, 5/5, 5/5 (`v1-r1..r3`, Luna) | Validated (v1). Befores fixed both bugs and added tests in one patch, never saw a test fail. Afters loaded the skill, two red/green/done rounds each |
| 3 | 1/4, 1/4, 1/4 (`v1-r1..r3`, Luna) | 4/4, 4/4, 4/4 (`v1-r1..r3`, Luna) | Validated (v1). Befores found the drift (item 1) and rewrote the doc. Afters used only view/glob/rg |

## Not counted

| Run | Why |
|---|---|
| `s01-after-calib-ref`, `s01-before-calib-base` | Scorer calibration (reference solution 6/6, untouched 0/6), not agent runs |
| `s00-after-gpt-5.6-luna-r1` | Harness fault: `prepare` pre-built the clone, so the agent's incremental `dotnet build` printed `0 Warning(s)`. Fixed (`prepare` now only restores) |
| `s01-after-calib-ref2`, `s01-before-calib-base2` | Step 1 re-calibration after the `/Agent` base app went in (6/6, 0/6) |
| `s02-before-gpt-6.1-sol-r1`, `-r2` (5/6, 5/6), `s02-after-gpt-5.6-luna-r1`, `-r2` (5/6, 6/6), `s02-*-calib-ref`, `-base` | Step 2 design v1, superseded. The Befores got items 1–5: they found `docs/agent-commands.md` through a link in the architecture doc's `/Agent` row, and a strong model writes "Deleted 2 completed tasks." unprompted. Only item 6 separated the arms. v2 drops the link and replaces item 3 with "the confirm prompt says how many" (an optional `Preview` on `Intent` that the code doesn't use yet). After r1 loaded the skill and ran `try.cs` but pasted the output without the `> ` prompts; v1's scorer wanted `> clear completed` |
| `s02-after-gpt-5.6-luna-v2r1`, `-v2r2` (5/6, 5/6) | Skill v2, superseded by one sentence. Both loaded the skill and ran `try.cs`, then summarised it ("try script confirmed") instead of pasting it. The skill now says to paste the output unchanged. The v2 Befores still count: the Before arm doesn't have the skill |
| `s02-after-calib-ref2`, `s02-before-calib-base2`, `s01-after-calib-ref3` | Calibration for Step 2 v2 (6/6, 0/6) and Step 1 after its patch context changed (6/6) |
| `s01-before-gpt-6.1-sol-r3` (1/6), `s01-after-gpt-5.6-luna-r3` (6/6) | Step 1 regression check after the `/Agent` base app: same result as the counted runs. Not added to the table, which stays at 2 runs per arm |
| `s01-*-calib-calib-ref-1117212852`, `s01-*-calib-base-…`, `s02-*` (same stamp) | Calibration with the new one-build scorer (6/6, 0/6, 6/6, 0/6), also scored with the old code to confirm no change (old-code scores not kept in `results.jsonl`) |
| `s03-after-calib-ref-1117213714` (4/4), `s03-before-calib-base-1117213714` (1/4) | Step 3 calibration. Untouched keeps item 2 (no files changed), as designed |
| `s03-after-gpt-5.6-luna-probe-fix` | Probe: reviewer told "Fix it ...: edit the file." Same report, nothing changed, only view/glob. Evidence that the tool list holds |
| `s03-before-gpt-5.6-luna-probe-ask1`, `-ask2` | Probe: default agent with "Don't change any files." added. Both changed nothing. Reported honestly in the walkthrough: the request holds for a short direct prompt |
| `s03-after-gpt-5.6-luna-probe-notools1..3` | Control probe (after the independent review): agent file with the `tools:` line deleted, prompt "...: edit the file." 2 of 3 edited the doc (`apply_patch`), against 0 of 2 with the tool list (`probe-fix`, `probe-sed`) |
| `s03-after-gpt-5.6-luna-probe-sed` | Probe: reviewer told to use `sed -i` if edit tools fail. Only view/glob; nothing changed |
| `s0{1,2,3,4}-*-calib-*-1117221057` | Regression calibration after the Dashboard base change (`f1fbf64`): s01 6/6 0/6, s02 6/6 0/6, s03 4/4 1/4; Step 4's own calibration 5/5 0/5 |
| `s00-before-gpt-6.1-sol-r1` (3/5), `s00-after-gpt-5.6-luna-r2` (1/5) | Step 0 decided VS Code only: in the CLI the agent answers by reading and installing lint rules doesn't change that. Kept as the evidence for the decision. First score of the Before (2/5) superseded by a scorer fix (CA1829 word order) |

## Scorer fixes

- Step 4, items 3 and 4: a long shell command can return early and print its output in a later `read_bash`
  call (After r2's first `./lab tdd green`). New `common.shell_runs()` joins those outputs to their command.
  Re-scored all 8 Step 4 scorings: After r2 4/5 → 5/5, the rest unchanged.

- Step 3 (after the independent review, before any write-up was final): the `Total:` line is read only
  after the report (AGENTS.md also asks for lint's `Total:`); numbered lists, `+` bullets, continuation
  lines, markdown-link and backslash paths accepted; a cited path must be a file or folder under `src/` or
  `tests/` (not the doc, not a bare name or route); `/Reports` pattern no longer matches any capitalised
  "Reports". All 8 Step 3 scorings re-scored: unchanged (Before 1/4 ×3, After 4/4 ×3, ref 4/4, base 1/4).

- Step 0, CA1829: also match "use `x.Count` rather than `Count()`" (word order). Re-scored the affected run.
- Step 2, item 6: match "clear completed" and "Type yes to confirm" inside one fenced block of the final message,
  not the exact `> clear completed` line. Agents trim the script's output. Applies from design v2; v1 runs not re-scored.
