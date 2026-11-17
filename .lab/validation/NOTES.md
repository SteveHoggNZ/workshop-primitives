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

## Not counted

| Run | Why |
|---|---|
| `s01-after-calib-ref`, `s01-before-calib-base` | Scorer calibration (reference solution 6/6, untouched 0/6), not agent runs |
| `s00-after-gpt-5.6-luna-r1` | Harness fault: `prepare` pre-built the clone, so the agent's incremental `dotnet build` printed `0 Warning(s)`. Fixed (`prepare` now only restores) |
| `s00-before-gpt-6.1-sol-r1` (3/5), `s00-after-gpt-5.6-luna-r2` (1/5) | Step 0 decided VS Code only: in the CLI the agent answers by reading and installing lint rules doesn't change that. Kept as the evidence for the decision. First score of the Before (2/5) superseded by a scorer fix (CA1829 word order) |

## Scorer fixes

- Step 0, CA1829: also match "use `x.Count` rather than `Count()`" (word order). Re-scored the affected run.
