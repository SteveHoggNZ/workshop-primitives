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
    ("no /Reports page", r"/Reports|Reports\.cshtml|Reports (page|route)|no Reports", r"^-.*Reports"),
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


ITEM = re.compile(r"^\s*(\d+[.)]|[-*+])\s+\S")
TOTAL = re.compile(r"^\W{0,4}Total\b[^\d\n]{0,12}(\d+)", re.M | re.I)


def _sections(final):
    """Items under ## Wrong and ## Missing (bullets or numbered, continuation lines joined), and the number on
    the Total line after them (None if missing). Only a Total after the report counts: AGENTS.md also asks for
    lint's `Total:` line, which may come earlier."""
    items = []
    current = None
    seen_report = False
    total = None
    for line in final.splitlines():
        st = line.strip()
        if re.match(r"^#{2,3}\s*\**(Wrong|Missing)\b", st, re.I):
            current, seen_report = True, True
            continue
        t = TOTAL.match(st)
        if t and seen_report:
            total = int(t.group(1))
            current = None
            continue
        if re.match(r"^#{1,3}\s", st):
            current = None
        if not current:
            continue
        if ITEM.match(line):
            items.append(line)
        elif st and items and line[:1] in " \t":
            items[-1] += " " + st
    return items, total


def _paths(item):
    """Path-like tokens in an item: backticked text, markdown link targets, and bare paths with an extension."""
    toks = re.findall(r"`([^`]+)`", item) + re.findall(r"\]\(([^)\s]+)\)", item)
    toks += re.findall(r"(?<![\w`/])((?:src|tests)[\\/][\w./\\-]+|[\w./\\-]+\.(?:cs|cshtml))", item)
    return [t.replace("\\", "/").split("#")[0] for t in toks]


def _resolves(ev, token, tracked):
    """A file or folder of the app or its tests (not the doc under review, not a bare name or route)."""
    t = token.strip().lstrip("./").split(":")[0].rstrip("/")
    if not t or " " in t or t.startswith("docs/") or "/" not in t and "." not in t:
        return False
    hits = [f for f in tracked if f == t or f.endswith("/" + t) or f.startswith(t + "/") or ("/" + t + "/") in ("/" + f)]
    return any(f.startswith(("src/", "tests/")) for f in hits)


def primitive_used(ev):
    """The reviewer has no edit or shell tools: an After run that used only read/search tools used it."""
    used = {name for _, name, _ in calls(ev)}
    if not used:
        return "n/a"
    return "yes" if used <= READ_ONLY_TOOLS else f"no ({', '.join(sorted(used - READ_ONLY_TOOLS))})"


def score(ev):
    final = final_message(ev)
    found = _found(ev, final)
    changed = ev.changed()
    fmt = bool(re.search(r"^#{2,3}\s*Wrong", final, re.M | re.I) and re.search(r"^#{2,3}\s*Missing", final, re.M | re.I)
               and _sections(final)[1] is not None)
    bullets, total = _sections(final)
    tracked = ev.git("ls-files").split()
    cited = [b for b in bullets if any(_resolves(ev, tok, tracked) for tok in _paths(b))]
    checkable = bool(bullets) and len(cited) == len(bullets) and total == len(bullets)
    return [
        ("finds at least 5 of the 7 drift items (report or doc fix)", len(found) >= 5, f"{len(found)}/7: {'; '.join(found)}"),
        ("no files changed", not changed, ", ".join(changed[:5])),
        ("report in the fixed format (## Wrong, ## Missing, Total: N)", fmt, ""),
        ("every item cites a path that exists; Total matches the items", checkable,
         f"{len(cited)}/{len(bullets)} items cite a real path; Total {total}"),
    ]
