---
name: dotnet-diagnostics
description: "Measure before you fix: time a slow page in-process and sample where its CPU time goes, with scripts/profile.cs. Use when something is slow, laggy or CPU-heavy, or when asked how much faster a change made it. Not for wrong output, errors or crashes (use the tdd skill), and not for visual glitches."
---

# .NET diagnostics: measure first

The team's rule: **never guess at a performance problem.** Measure, fix what the numbers point to,
then measure again **with the same command** and compare. A fix in the wrong place doesn't move the
numbers, and "it should be much faster now" isn't a number.

## Measure

The script hosts the app in-process (no server, no port, no installs), runs the setup requests once,
times the page `--repeat` times, and samples where the CPU time goes:

```bash
dotnet run --no-cache .agents/skills/dotnet-diagnostics/scripts/profile.cs -- \
    --setup "POST /Bulk?handler=Generate" --path "/Bulk?q=sample task 12" --repeat 5
```

- `--setup` runs once first (repeat it for several); `--path` is the page you time. A request without
  `POST ` is a GET.
- The first run builds the script and restores two diagnostics packages: about 30 s. Later runs: about 10 s.
- Read `latency ms: median ..., max ...`, then **app methods by own time**: the top row is where the
  time goes. Open that method before you change anything.

## Fix

- Change the method the profile points to, and nothing else. Keep the behaviour the same: the
  existing tests must still pass (`dotnet test`).

## Measure again

- Run **the same command** again. The top row should have moved, and the latency should have dropped.

## Report

In your final message, paste the two `latency ms:` lines (before and after) and the top line of
"app methods by own time" from each run, unchanged, in a code block. Then say how many times faster
it is, from those numbers. A reader should be able to see the measurement, not take your word for it.
