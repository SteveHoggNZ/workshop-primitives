"""Step 2: a skill with a tested script. A "clear completed" command for the in-app agent at /Agent."""

import re

from common import final_message

PROMPTS = {
    "before": 'Add a command to the in-app agent at /Agent that deletes all completed tasks. '
              'Users should be able to type "clear completed".',
}
PROMPTS["after"] = PROMPTS["before"]

# Stub transcripts for `lab.py calibrate` (no agent run): the reference pasted the try script's conversation.
CALIB = {"ref": '### Copilot\n\nAdded the command.\n\n```\n> help\nI understand:\n  add <title>   (e.g. add buy milk)\n  list   (e.g. list)\n  done <id>   (e.g. done seed3)\n  delete <id>   (e.g. delete seed3)\n  clear completed   (e.g. clear completed)\n\n> clear completed\nThis will delete 2 completed tasks. Type yes to confirm.\n\n> yes\nDeleted 2 completed tasks.\n\n> list\nseed3  [ ] Order coffee\nseed4  [ ] Send invitations\nseed5  [ ] Print name badges\nseed6  [ ] Test the projector\n```\n'}


def score(ev):
    t = ev.dotnet_test("FullyQualifiedName!~LabCheck")
    c = ev.run_checks("LabCheck02")["results"]
    passed = lambda name: c.get(name) == "Passed"
    tests = [f for f in ev.changed() if f.startswith("tests/") and f.endswith(".cs")]
    has_test = any("clear completed" in ev.read(f).lower() for f in tests)
    docs = bool(re.search(r"^\+.*clear completed", ev.diff("docs/agent-commands.md"), re.I | re.M))
    final = final_message(ev)
    # The script's conversation pasted in a code block. Agents trim it (r1 dropped the "> " prompts), so match the
    # command and the confirm reply inside one fenced block, not the exact transcript format.
    blocks = re.findall(r"```[^\n]*\n(.*?)```", final, re.S)
    live = any(re.search(r"clear completed", b, re.I) and re.search(r"type yes to confirm", b, re.I) for b in blocks)
    return [
        ('"clear completed" deletes only completed tasks', passed("Clears_only_completed"), ""),
        ("asks first; \"no\" keeps the tasks (team rule)", passed("Asks_first_and_no_cancels"), ""),
        ("confirm prompt says how many will be deleted", passed("Confirm_says_how_many"), ""),
        ("test for the command, suite passes", has_test and t["ok"], f"{tests}; {t['summary']}"),
        ("docs/agent-commands.md has the command", docs, ""),
        ("final message shows a live run (clear completed + confirm reply in a code block)", live, ""),
    ]
