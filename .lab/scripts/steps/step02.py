"""Step 2: a skill with a tested script. A "clear completed" command for the in-app agent at /Agent."""

import re

from common import final_message

PROMPTS = {
    "before": 'Add a command to the in-app agent at /Agent that deletes all completed tasks. '
              'Users should be able to type "clear completed".',
}
PROMPTS["after"] = PROMPTS["before"]


def score(ev):
    t = ev.dotnet_test("FullyQualifiedName!~LabCheck")
    c = ev.run_checks("LabCheck02")["results"]
    passed = lambda name: c.get(name) == "Passed"
    tests = [f for f in ev.changed() if f.startswith("tests/") and f.endswith(".cs")]
    has_test = any("clear completed" in ev.read(f).lower() for f in tests)
    docs = bool(re.search(r"^\+.*clear completed", ev.diff("docs/agent-commands.md"), re.I | re.M))
    final = final_message(ev)
    live = bool(re.search(r">\s*`?clear completed", final, re.I)) and "type yes to confirm" in final.lower()
    return [
        ('"clear completed" deletes only completed tasks', passed("Clears_only_completed"), ""),
        ("asks first; \"no\" keeps the tasks (team rule)", passed("Asks_first_and_no_cancels"), ""),
        ("reply says how many were deleted", passed("Reply_says_how_many"), ""),
        ("test for the command, suite passes", has_test and t["ok"], f"{tests}; {t['summary']}"),
        ("docs/agent-commands.md has the command", docs, ""),
        ("final message shows a live run (> clear completed, confirm prompt)", live, ""),
    ]
