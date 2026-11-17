"""Step 5: a diagnostics skill (measure first). Slow title search on /Bulk with 10,000 tasks.

Items (baseline = both arms should pass; lesson = only the primitive supplies it):
  1 baseline  fixed: at least 5x faster than the original code on the same machine, same counts, suite passes
  2 lesson    profiled before the first edit to src/ (a profiler run, not just a timer)
  3 lesson    timed before the first src/ edit and again after the last one
  4 lesson    the before/after numbers in the final message come from runs in the transcript
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
TIMING_OUT = re.compile(r"latency ms|\bmedian\b[^\n]*\d|\b\d+(\.\d+)?\s?ms\b|Elapsed|elapsed", re.I)


def _timing_runs(ev):
    out = []
    for pos, cmd, body in shell_runs(ev):
        if re.match(r"\s*(cd \S+ && )?dotnet (test|build)\b", cmd) and not PROFILER.search(cmd):
            continue  # test and build durations aren't a measurement of the page
        if PROFILER.search(cmd) or (TIMING_OUT.search(body[len(cmd):]) and re.search(r"Stopwatch|curl|time |dotnet run|bench|\.cs", cmd)):
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
    """Latency-like numbers: 1039.6, 33 ms, 1,040ms. Two significant figures at least, so '5x' doesn't count."""
    return {n.replace(",", "") for n in re.findall(r"(\d[\d,]*\.?\d*)\s?ms", text) if len(n.replace(",", "").replace(".", "")) >= 2}


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
    measured = set()
    for _, cmd, body in runs:
        measured |= _numbers(body[len(cmd):])
    reported = _numbers(final) & measured
    return [
        ("fixed: 5x faster than the original on this machine, same counts, tests pass", fixed,
         f"{c['results']}; {t['summary']}"),
        ("profiled before the first edit to src/", profiled_first, f"{len(runs)} timing runs; first src edit @{first}"),
        ("timed before the first src/ edit and after the last", before_after, f"edits @{first}..{last}"),
        ("final message's numbers come from measured runs", len(reported) >= 2, f"{sorted(reported)[:4]}"),
    ]
