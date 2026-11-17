# Validation notes

Results live in `results.jsonl` (one line per scored run); `python3 .lab/scripts/lab.py results` prints them.
Every run that isn't counted is listed here with the reason. Never drop a run without a line here.

## Setup

- Copilot CLI 1.0.112 on macOS (arm64), .NET SDK 10.0.102, 17 Nov 2026.
- Before on `gpt-6.1-sol` (strong), After on `gpt-5.6-luna` (small). 2 runs per arm (decided 17 Nov).
- Clones in `../.lab-runs/`, built with `./lab goto`, `.lab/` removed; `COPILOT_HOME` isolated in
  `../.lab-homes/<model>`; `--allow-all --trust-folder --share`.

## Counted

| Step | Before | After | Verdict |
|---|---|---|---|
| 1 | 1/6, 1/6 | 6/6, 6/6 | Validated. No flips |
| 2 | 3/6, 3/6 (`v2r1`, `v2r2`) | 6/6, 6/6 (`v3r1`, `v3r2`) | Validated. Befores pass 1, 2, 4. Afters loaded the skill and ran `try.cs` |

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
| `s00-before-gpt-6.1-sol-r1` (3/5), `s00-after-gpt-5.6-luna-r2` (1/5) | Step 0 decided VS Code only: in the CLI the agent answers by reading and installing lint rules doesn't change that. Kept as the evidence for the decision. First score of the Before (2/5) superseded by a scorer fix (CA1829 word order) |

## Scorer fixes

- Step 0, CA1829: also match "use `x.Count` rather than `Count()`" (word order). Re-scored the affected run.
- Step 2, item 6: match "clear completed" and "Type yes to confirm" inside one fenced block of the final message,
  not the exact `> clear completed` line. Agents trim the script's output. Applies from design v2; v1 runs not re-scored.
