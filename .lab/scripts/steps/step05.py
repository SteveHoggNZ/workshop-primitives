"""Step 5: a diagnostics skill (measure first). Slow title search on /Bulk with 10,000 tasks.

Items (baseline = both arms should pass; lesson = only the primitive supplies it):
  1 baseline  fixed: at least 5x faster than the original code on the same machine, same counts, suite passes
  2 baseline  timed before the first src/ edit and again after the last one (the prompt asks how much faster)
  3 lesson    profiled before the first edit to src/ (a profiler run that says where the time goes, not a timer)
  4 lesson    the final message shows where the time went: a method with its measured share, from a profiler run

Design v2 (after the pilot): v1 had item 2 as a lesson and item 4 as "the numbers come from runs". The pilot
Before timed with curl before and after (the prompt asks for it) and reported "~4.0s to ~0.1s": a measurement,
paraphrased. v2 makes timing a baseline and scores what only a profile gives: where the time went.
"""

import re

from common import final_message, first_edit, shell_runs, calls, EDIT_TOOLS

PROMPTS = {"before": "Search on /Bulk is very slow once you click 'Generate 10,000 sample tasks': each search takes "
                     "about a second. Make it fast, and tell me how much faster it is."}
PROMPTS["after"] = PROMPTS["before"]
CHECKS = ["LabCheck05"]

# A profiler shows where the time goes. Our script, or the usual .NET tools if an agent has them.
PROFILER = re.compile(r"profile\.cs|dotnet-trace|dotnet-counters|EventPipe|dotnet-monitor|CPU samples", re.I)
# A timing run: a profiler, or a command that prints a latency (a stopwatch script, curl -w, time).
# Commands that time by themselves (found in v2 Before r1: curl -w %{time_total} prints seconds, no "ms").
TIMING_CMD = re.compile(r"time_total|time_starttransfer|/usr/bin/time|(^|[;&|]\s*)time\s|Measure-Command|hyperfine|Stopwatch")
TIMING_OUT = re.compile(r"latency ms|\bmedian\b[^\n]*\d|\b\d+(\.\d+)?\s?ms\b|Elapsed|elapsed", re.I)


def _timing_runs(ev):
    out = []
    for pos, cmd, body in shell_runs(ev):
        if re.match(r"\s*(cd \S+ && )?dotnet (test|build)\b", cmd) and not PROFILER.search(cmd):
            continue  # test and build durations aren't a measurement of the page
        if PROFILER.search(cmd) or TIMING_CMD.search(cmd) or (TIMING_OUT.search(body[len(cmd):]) and re.search(r"Stopwatch|curl|dotnet run|bench|\.cs", cmd)):
            out.append((pos, cmd, body))
    return out


def _src_edits(ev):
    edits = []
    for pos, name, body in calls(ev):
        if "Denied by preToolUse hook" in body:
            continue
        if name in EDIT_TOOLS and "src/Tasks.App/" in body.replace("\\", "/"):
            edits.append(pos)
    first = first_edit(ev, "src/Tasks.App/")
    if first is not None:
        edits.append(first)
    return sorted(edits)


def _numbers(text):
    """Latency-like numbers on lines that mention ms ("latency ms: median 1039.6", "33 ms", "1,040ms").
    At least two digits, so "5x" or a step number doesn't count."""
    out = set()
    for line in text.splitlines():
        if re.search(r"\bms\b|\dms\b|millisecond", line, re.I):
            out |= {n.replace(",", "") for n in re.findall(r"\d[\d,]*(?:\.\d+)?", line) if len(n.replace(",", "").replace(".", "")) >= 2}
    return out


def primitive_used(ev):
    loaded = any(name == "skill" and "diagnostics" in body[:300] for _, name, body in calls(ev))
    return "yes" if loaded or ev.ran("profile.cs") else "no"


CALIB = {"ref": """### `bash`

`$ dotnet run --no-cache .agents/skills/dotnet-diagnostics/scripts/profile.cs -- --setup "POST /Bulk?handler=Generate" --path "/Bulk?q=sample task 12"`

latency ms: median 1039.6, max 1067.6
app methods by own time (the deepest app method on the stack: look here first):
   99.8%  Tasks.App.Services.TextHelpers.Normalise(class System.String)

### `apply_patch`

*** Update File: /x/src/Tasks.App/Services/TextHelpers.cs

### `bash`

`$ dotnet run --no-cache .agents/skills/dotnet-diagnostics/scripts/profile.cs -- --setup "POST /Bulk?handler=Generate" --path "/Bulk?q=sample task 12"`

latency ms: median 32.8, max 40.9

### Copilot

```text
before: latency ms: median 1039.6, max 1067.6
   99.8%  Tasks.App.Services.TextHelpers.Normalise(class System.String)
after:  latency ms: median 32.8, max 40.9
```

About 32 times faster.
"""}


def score(ev):
    t = ev.dotnet_test("FullyQualifiedName!~LabCheck")
    c = ev.run_checks("LabCheck05")
    fixed = c["ok"] and t["ok"]
    edits = _src_edits(ev)
    first, last = (edits[0], edits[-1]) if edits else (None, None)
    runs = _timing_runs(ev)
    profiled_first = any(PROFILER.search(cmd) and (first is None or pos < first) for pos, cmd, _ in runs)
    before_after = first is not None and any(pos < first for pos, _, _ in runs) and any(pos > last for pos, _, _ in runs)
    final = final_message(ev)
    profiled = "\n".join(body for _, cmd, body in runs if PROFILER.search(cmd) or PROFILER.search(body))
    shares = {m.group(1) for line in final.splitlines() if re.search(r"[A-Z]\w+\.\w+|\w+\(", line)
              for m in re.finditer(r"(\d{1,3}(?:\.\d)?)\s?%", line)}
    shown = sorted(x for x in shares if re.search(r"(?<![\d.])" + re.escape(x) + r"\s?%", profiled))
    return [
        ("fixed: 5x faster than the original on this machine, same counts, tests pass", fixed,
         f"{c['results']}; {t['summary']}"),
        ("timed before the first src/ edit and after the last", before_after, f"{len(runs)} timing runs; edits @{first}..{last}"),
        ("profiled before the first edit to src/", profiled_first, f"first src edit @{first}"),
        ("final message shows where the time went (a method and its measured share)", bool(shown), f"{shown[:3]}"),
    ]
