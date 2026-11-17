---
name: lab-wizard-session
description: "Use when the lab wizard scores a Before or After run: find the participant's run transcript and extract its final message and commands run."
---

# Lab wizard session

Finds the participant's Before or After run in the local Copilot session store, so you can score
items the diff can't show (the agent's answer, a quoted `Total:` line, whether `./lab lint` ran)
without asking them to paste anything.

## Procedure

1. Get the step's prompt from its walkthrough (`.lab/walkthroughs/stepNN.md`), exactly as you
   showed it to the participant.
2. Get your own session ID so your session is excluded: the `COPILOT_AGENT_SESSION_ID` environment
   variable, or the folder name of your session folder under `~/.copilot/session-state/`.
3. Run the script, passing the prompt on stdin (`-`) so multi-line prompts survive quoting:

   ```bash
   python3 .lab/skills/lab-wizard-session/scripts/find-session.py - "$COPILOT_AGENT_SESSION_ID" <<'EOF'
   <the step's prompt>
   EOF
   ```

   On Windows use `python` and pipe the prompt in, for example
   `@'...'@ | python .lab\skills\lab-wizard-session\scripts\find-session.py - $env:COPILOT_AGENT_SESSION_ID`.
4. Read the output:
   - `Session:` the run's session ID. The newest match wins, so run the script right after the
     participant says the run finished, and check it is the run you expect (Before vs After).
   - `=== Final message ===` the agent's last non-empty reply. Score answer and `Total:` items from it.
   - `=== Commands run ===` every command the agent ran, for example `[bash] ./lab lint`.
     A quoted `Total:` line with no `./lab lint` here was not actually run.
5. Quote the evidence you used in the scoring table (session ID, the line from the final message,
   the command).

## If nothing is found

The script exits non-zero when no transcript matches. That happens on another OS or tool, or when
VS Code stores the session somewhere else. Don't search further: **ask the participant to paste the
agent's final message** and score from that.
