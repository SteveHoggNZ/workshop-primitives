"""Step 4: a TDD skill with phase state. Two dashboard bugs; was a test seen failing before src/ changed?

Items (baseline = both arms should pass; lesson = only the primitive supplies it):
  1 baseline  both bugs fixed (hidden checks through the page), visible suite passes
  2 baseline  the agent's tests cover both bugs (empty dashboard / 0%, and the week start)
  3 lesson    a test run failed on an assertion before the first edit to src/ (red first, from the transcript)
  4 lesson    ./lab tdd was used: a GREEN (a real failure recorded) and a DONE (suite passes)
  5 lesson    the final message pastes the failure the test caught (a FAIL/assertion line in a code block)
"""

import re

from common import calls, final_message, first_edit, shell_runs

BUGS = ("The dashboard has two bugs. 1) /Dashboard returns a 500 error when there are no tasks; it "
        "should show 0%. 2) On a Monday morning in Auckland (for example 2026-11-16 08:00 +13:00) "
        "'Week starting' shows the previous Monday (9 Nov) instead of 16 Nov.")
PROMPTS = {"before": BUGS + " Fix both."}
PROMPTS["after"] = PROMPTS["before"]
CHECKS = ["LabCheck04"]

RED = re.compile(r"Failed!\s*-\s*Failed:\s*[1-9]|GREEN: \d+ failing|\[FAIL\]|^\s*Failed Tasks\.App\.Tests\.|failed: [1-9]|error TESTERROR", re.M)


def _red_runs(ev):
    """Positions of shell calls whose output shows a test failing (not a build error)."""
    out = []
    for pos, cmd, body in shell_runs(ev):
        if re.search(r"dotnet test|lab tdd green", cmd) and RED.search(body) and "error CS" not in body:
            out.append(pos)
    return out


def _src_edit(ev):
    return first_edit(ev, "src/Tasks.App/")


def _tdd(ev, sub, ok_word):
    return any(f"lab tdd {sub}" in cmd and ok_word in body for _, cmd, body in shell_runs(ev))


def primitive_used(ev):
    loaded = any(name == "skill" and "tdd" in body[:300] or "skills/tdd/SKILL.md" in body[:600] for _, name, body in calls(ev))
    return "yes" if loaded or ev.ran("lab tdd red") else "no"


CALIB = {"ref": """### `bash`

**Red**

`$ ./lab tdd red "empty dashboard shows 0%"`

RED: write one failing test

### `bash`

`$ ./lab tdd green`

GREEN: 1 failing, for the right reason:
  FAIL Tasks.App.Tests.DashboardTests.Empty_dashboard_shows_zero: Assert.Equal() Failure: Expected: OK Actual: InternalServerError

### `apply_patch`

*** Update File: src/Tasks.App/Services/TaskStats.cs

### `bash`

`$ ./lab tdd done`

DONE: empty dashboard shows 0%.

### Copilot

Fixed both.

```text
FAIL Tasks.App.Tests.DashboardTests.Empty_dashboard_shows_zero: Assert.Equal() Failure: Expected: OK Actual: InternalServerError
```
"""}


def score(ev):
    t = ev.dotnet_test("FullyQualifiedName!~LabCheck")
    c = ev.run_checks("LabCheck04")["results"]
    fixed = c.get("Empty_dashboard_shows_zero_percent") == "Passed" and \
        [v for k, v in c.items() if k.startswith("Week_starts_on_the_local_monday")] == ["Passed"] * 3
    added = "\n".join(l for l in ev.diff("tests").splitlines() if l.startswith("+"))
    covers = bool(re.search(r"0\s?%|Percent|[Ee]mpty|no tasks|NoTasks", added)) and bool(re.search(r"Week|16 Nov|WeekStart", added, re.I))
    reds = _red_runs(ev)
    edit_at = _src_edit(ev)
    red_first = bool(reds) and (edit_at is None or reds[0] < edit_at)
    tdd = _tdd(ev, "green", "GREEN:") and _tdd(ev, "done", "DONE:")
    blocks = re.findall(r"```[^\n]*\n(.*?)```", final_message(ev), re.S)
    pasted = any(re.search(r"\bFAIL\b|Assert\.\w+\(\) Failure|Failed Tasks\.App", b) for b in blocks)
    return [
        ("both bugs fixed (hidden checks), suite passes", fixed and t["ok"], f"{c}; {t['summary']}"),
        ("tests cover both bugs", covers, ""),
        ("a test failed before src/ was first edited", red_first, f"red@{reds[:1]} edit@{edit_at}"),
        ("./lab tdd used: GREEN then DONE", tdd, ""),
        ("final message pastes the failure the test caught", pasted, ""),
    ]
