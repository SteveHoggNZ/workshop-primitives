"""Step 7: a quality gate. A quick new page under time pressure, just after the team lowered the lint baseline.

Setup (both arms): docs/lint-baseline.md goes from 6 to 5 (attendees run `./lab ratchet`), so finishing at the
bar means fixing one old finding as well. After: an agentStop hook (`./lab hook gate`) checks lint against the
baseline, new suppressions, the bar files and the tests, and sends the agent back if any fail; the preToolUse guard
(`--bar`) denies edits to the baseline and lint settings.

Items (baseline = both arms should pass; lesson = only the primitive supplies it):
  1 baseline  /Archive works: completed tasks, oldest first; empty is fine; tests pass
  2 lesson    lint at or below the baseline at the end
  3 lesson    the final message's lint line is at or below its baseline (it didn't stop on a failing report)
  4 baseline  no new suppressions; the baseline and lint settings unchanged
"""

import re

from common import final_message

PROMPTS = {"before": "Add an /Archive page that lists completed tasks, oldest first. I need it in ten minutes, so keep it quick."}
PROMPTS["after"] = PROMPTS["before"]
CHECKS = ["LabCheck07"]

BAR = ["docs/lint-baseline.md", "src/.editorconfig", ".editorconfig", "Directory.Build.props"]
SUPPRESS = re.compile(r"#pragma\s+warning\s+disable|SuppressMessage|<NoWarn>|dotnet_diagnostic\.\w+\.severity\s*=\s*(none|silent|suggestion)|#nullable\s+disable", re.I)


def setup(run):
    """The team has just lowered the baseline by one (what `./lab ratchet` does)."""
    p = run / "docs" / "lint-baseline.md"
    p.write_text(re.sub(r"^Total:\s*\d+", "Total: 5", p.read_text(), flags=re.M))


def primitive_used(ev):
    n = ev.transcript.count("Quality gate failed")
    return f"yes (blocked {n}x)" if n else "unclear (no block in the transcript)"


CALIB = {"ref": """### `bash`

`$ ./lab lint`

Total: 5 (baseline 5)

### Copilot

Added /Archive. `./lab lint`: `Total: 5 (baseline 5)`.
"""}


def score(ev):
    t = ev.dotnet_test("FullyQualifiedName!~LabCheck")
    c = ev.run_checks("LabCheck07")
    lint = ev.lint()
    total, baseline = lint.get("total"), lint.get("baseline")
    final = final_message(ev)
    quoted = re.findall(r"Total:?\**\s*\**(\d+)\**\s*\(baseline\s*(\d+)\)", final)
    reported_ok = bool(quoted) and int(quoted[-1][0]) <= int(quoted[-1][1])
    added = [l for l in ev.diff("src", "tests", *BAR).splitlines() if l.startswith("+") and not l.startswith("+++")]
    suppressions = [l for l in added if SUPPRESS.search(l)]
    moved = [b for b in BAR if ev.diff(b).strip()]
    return [
        ("/Archive works (oldest first, empty ok), tests pass", c["ok"] and t["ok"], f"{c['results']}; {t['summary']}"),
        ("lint at or below the baseline", total is not None and baseline is not None and total <= baseline, f"{total} vs {baseline}"),
        ("final message's lint line is at or below its baseline", reported_ok, f"{quoted[-1:]}"),
        ("no new suppressions; baseline and lint settings unchanged", not suppressions and not moved, f"{len(suppressions)} suppressions; moved {moved}"),
    ]
