"""Step 8: spec-driven development. A vague backlog item (B1), scored by 8 hidden acceptance tests.

Three arms, same model, same backlog item:
  before  one prompt
  ask     the same prompt + "ask me first, then stop"; the facilitator answers only what was asked, from
          .lab/facilitator/step08/answer-key.md, then says "now implement it" (same session)
  after   Spec Kit + the constitution: /speckit-specify, /speckit-clarify (answer each question from the key),
          /speckit-plan, /speckit-tasks, /speckit-implement, a fresh session per phase
The harness runs turn 1; later turns go through `lab.py run ID --append --prompt ...` (with --flags=--continue
inside a phase). All 8 items are acceptance tests: each is a decision the backlog leaves open (priority values
and default, field names, sort ties, inclusive filter, local "today", 422, the page).
"""

PROMPT = "Implement backlog item B1 from docs/backlog.md."
PROMPTS = {
    "before": PROMPT,
    "ask": PROMPT + " Before you write any code, ask me the questions you need answered, then stop and wait for my answers.",
    "after": "/speckit-specify " + PROMPT,
}
CHECKS = ["LabCheck08"]

TESTS = [
    ("B1_1_defaults", "defaults: priority normal, dueDate null"),
    ("B1_2_create_with_priority_and_due_date", "create with priority and dueDate"),
    ("B1_3_sort_by_priority_with_tie_breaks", "sort=priority with tie breaks"),
    ("B1_4_filter_by_priority", "priority filter, creation order"),
    ("B1_5_due_before_is_inclusive_and_skips_undated", "due_before inclusive, skips undated"),
    ("B1_6_overdue_uses_local_today", "overdue uses today in Auckland"),
    ("B1_7_patch_clears_due_date_and_rejects_unknown_priority", "PATCH clears dueDate; unknown priority 422"),
    ("B1_8_task_list_page_shows_priority_and_overdue", "task list page shows priority and Overdue"),
]

CALIB = {"ref": "### Copilot\n\nImplemented B1.\n"}


def primitive_used(ev):
    if ev.meta["phase"] != "after":
        return "n/a"
    return "yes" if "speckit" in ev.transcript and (ev.dir / "specs").exists() else "no"


def score(ev):
    t = ev.dotnet_test("FullyQualifiedName!~LabCheck")
    c = ev.run_checks("LabCheck08")["results"]
    out = [(label, c.get(name) == "Passed", "") for name, label in TESTS]
    out[-1] = (out[-1][0], out[-1][1], f"own suite: {t['summary']}")
    return out
