"""Step 0: the linter. Same question before and after; after, the agent can read six diagnostics, not two."""

import re

from common import calls, final_message

PROMPTS = {"before": "What problems are there in src/Tasks.App/Pages/Recent.cshtml.cs?"}
PROMPTS["after"] = PROMPTS["before"]

FINDINGS = {
    "CA1305 culture-dependent DateTime.ToString": r"CA1305|IFormatProvider|format provider|(ToString\(\"HH:mm:ss\"\)|ToString).{0,200}(culture|locale)|(culture|locale).{0,200}ToString",
    "CA1304 ToUpper without culture": r"CA1304|ToUpper.{0,200}(culture|locale|Invariant)|(culture|locale).{0,200}ToUpper",
    "CA1829 Count() vs Count": r"CA1829|Count\(\).{0,200}(property|\.Count\b)|\.Count\b.{0,80}(rather than|instead of).{0,40}Count\(\)|use (the )?\.?Count property",
    "CA1822 Describe could be static": r"CA1822|Describe.{0,120}static|static.{0,120}Describe",
}


def score(ev):
    final = final_message(ev)
    checks = [(name, bool(re.search(rx, final, re.I | re.S)), "") for name, rx in FINDINGS.items()]
    build = [n for _, n, b in calls(ev) if re.search(r"dotnet build|lab lint|dotnet test|get_errors|problems", n + b)]
    checks.append(("used a deterministic check (build, ./lab lint, diagnostics)", bool(build), ", ".join(sorted(set(build)))[:120]))
    return checks
