"""Step 1: AGENTS.md + a scoped page rule. Team decisions the code doesn't show, on a new /Completed page."""

import re

from common import final_message, new_files

PROMPTS = {
    "before": "Add a /Completed page, like the Recent page, that lists completed tasks and shows when the page was loaded.",
}
PROMPTS["after"] = PROMPTS["before"]

# Stub transcripts for `lab.py calibrate` (no agent run): the reference ran lint and reported the total.
CALIB = {"ref": "### `bash`\n\n./lab lint\n\n### Copilot\n\nAdded the page.\n\nTotal: 6 (baseline 6)\n"}


def score(ev):
    pages = new_files(ev, "src/Tasks.App/Pages/", ".cs")
    src = "\n".join(ev.read(f) for f in pages)
    arch = ev.diff("docs/architecture.md")
    changelog = ev.diff("CHANGELOG.md")
    final = final_message(ev)
    t = ev.dotnet_test("FullyQualifiedName!~LabCheck")
    c = ev.run_checks("LabCheck01")["results"]
    passed = lambda name: c.get(name) == "Passed"
    follows_code = (bool(pages) and "TaskStore" not in src
                    and not re.search(r"DateTime(Offset)?\.(Now|UtcNow)", src)
                    and passed("Route_returns_200") and t["ok"])
    return [
        ("TaskService + TimeProvider, route works, tests pass", follows_code, f"{pages}; {t['summary']}"),
        ("load time shown with UTC offset (10:00 +13:00)", passed("Load_time_has_the_offset"), ""),
        ("docs/architecture.md lists /Completed", bool(re.search(r"^\+.*/completed", arch, re.I | re.M)), ""),
        ("CHANGELOG.md entry under Unreleased", bool(re.search(r"^\+.*completed", changelog, re.I | re.M)), ""),
        ("reported ./lab lint Total", ev.ran("lab lint") and bool(re.search(r"Total:?\s*\**\s*\d", final)), ""),
        ("page template conventions (data-page, task-row, _Empty)", passed("Template_conventions") and passed("Empty_state_uses_partial"), ""),
    ]
