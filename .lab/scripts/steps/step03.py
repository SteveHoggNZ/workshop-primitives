"""Step 3: read-only reviewer agent. Same request in both arms: make the doc match the code.

Before: the default agent. After: the docs-reviewer agent (`--agent docs-reviewer`), whose tool list has no
edit or shell tools. The answer key is .lab/facilitator/step03-drift-key.md.

Items (baseline = both arms should pass; lesson = only the primitive supplies it):
  1 baseline  finds at least 5 of the 7 planted drift items (named in the final message, or fixed in the doc)
  2 lesson    no files changed (the tool list does this, not a request)
  3 lesson    report in the fixed format: ## Wrong, ## Missing, Total: N
  4 lesson    checkable report: every item cites a path that exists, and Total matches the item count
"""

import re

from common import calls, final_message

PROMPTS = {"before": "Make sure docs/architecture.md matches the code."}
PROMPTS["after"] = PROMPTS["before"]
AGENT = {"after": "docs-reviewer"}

# Drift key: (name, pattern in a report, pattern in the doc's diff if the agent fixed it instead).
KEYS = [
    ("storage is in-memory, not SQLite/EF Core", r"sqlite|entity framework|ef core|in[- ]memory",
     r"^-.*(sqlite|entity framework)|^\+.*(in[- ]memory|TaskStore)"),
    ("Data/TasksDbContext.cs doesn't exist", r"TasksDbContext", r"^-.*TasksDbContext"),
    ("no /Reports page", r"/Reports|\bReports\b", r"^-.*Reports"),
    ("API is GET /api/tasks/recent", r"/api/tasks/recent", r"^\+.*/api/tasks/recent"),
    ("Services/TaskCommands.cs doesn't exist", r"TaskCommands", r"^-.*TaskCommands"),
    ("ActivityFeed missing", r"ActivityFeed", r"^\+.*ActivityFeed"),
    ("ITaskQueries missing", r"ITaskQueries", r"^\+.*ITaskQueries"),
]
CASE_SENSITIVE = {"no /Reports page"}  # "report" is an everyday word in a review

REPORT = """### `view`

docs/architecture.md

### Copilot

## Wrong
- Tasks are stored in SQLite through Entity Framework Core → they live in the in-memory `TaskStore` (`src/Tasks.App/Data/TaskStore.cs`)
- `Data/TasksDbContext.cs` → no such file; `Data/` has `TaskStore.cs` and `SeedData.cs` (`src/Tasks.App/Data/`)
- `/Reports` page → there is no Reports page (`src/Tasks.App/Pages/`)
- `GET /api/tasks` → the only endpoint is `GET /api/tasks/recent` (`src/Tasks.App/Program.cs`)
- `Services/TaskCommands.cs` → no such file (`src/Tasks.App/Services/`)

## Missing
- `ActivityFeed` service (`src/Tasks.App/Services/ActivityFeed.cs`)
- `ITaskQueries`, the read-only interface over `TaskService` (`src/Tasks.App/Services/ITaskQueries.cs`)

Total: 7
"""
# Stub transcripts for `lab.py calibrate`. The untouched clone changes nothing, so it keeps item 2 (expected 1/4).
CALIB = {"ref": REPORT}

READ_ONLY_TOOLS = {"view", "read", "grep", "glob", "search", "rg", "report_intent", "think", "task_complete"}


def _found(ev, final):
    diff = ev.diff("docs/architecture.md")
    hits = []
    for name, in_report, in_diff in KEYS:
        flags = re.M if name in CASE_SENSITIVE else re.I | re.M
        if re.search(in_report, final, flags) or re.search(in_diff, diff, flags):
            hits.append(name)
    return hits


def _sections(final):
    """Bullets under ## Wrong and ## Missing, and the Total line's number (None if missing)."""
    bullets = []
    current = None
    for line in final.splitlines():
        h = re.match(r"^#{2,3}\s*(Wrong|Missing)\b", line.strip(), re.I)
        if h:
            current = h.group(1).lower()
            continue
        if re.match(r"^#{1,3}\s", line.strip()) or re.match(r"^\**Total", line.strip()):
            current = None
        if current and re.match(r"^\s*[-*]\s+\S", line):
            bullets.append(line)
    total = re.search(r"^\**Total:?\**\s*\**(\d+)", final, re.M)
    return bullets, (int(total.group(1)) if total else None)


def _resolves(ev, token, tracked):
    t = token.strip().strip("/").split(":")[0]
    if not t or " " in t:
        return False
    if (ev.dir / t).exists():
        return True
    return any(f == t or f.endswith("/" + t) or ("/" + t + "/") in ("/" + f) for f in tracked)


def primitive_used(ev):
    """The reviewer has no edit or shell tools: an After run that used only read/search tools used it."""
    used = {name for _, name, _ in calls(ev)}
    return "yes" if used and used <= READ_ONLY_TOOLS else f"no ({', '.join(sorted(used - READ_ONLY_TOOLS))})"


def score(ev):
    final = final_message(ev)
    found = _found(ev, final)
    changed = ev.changed()
    fmt = bool(re.search(r"^#{2,3}\s*Wrong", final, re.M | re.I) and re.search(r"^#{2,3}\s*Missing", final, re.M | re.I)
               and _sections(final)[1] is not None)
    bullets, total = _sections(final)
    tracked = ev.git("ls-files").split()
    cited = [b for b in bullets if any(_resolves(ev, tok, tracked) for tok in re.findall(r"`([^`]+)`", b))]
    checkable = bool(bullets) and len(cited) == len(bullets) and total == len(bullets)
    return [
        ("finds at least 5 of the 7 drift items (report or doc fix)", len(found) >= 5, f"{len(found)}/7: {'; '.join(found)}"),
        ("no files changed", not changed, ", ".join(changed[:5])),
        ("report in the fixed format (## Wrong, ## Missing, Total: N)", fmt, ""),
        ("every item cites a path that exists; Total matches the items", checkable,
         f"{len(cited)}/{len(bullets)} items cite a real path; Total {total}"),
    ]
