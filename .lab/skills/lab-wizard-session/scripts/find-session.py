#!/usr/bin/env python3
"""Find the participant's Before/After run transcript and print what the lab wizard needs to score it.

Usage: find-session.py PROMPT OWN_SESSION_ID
  PROMPT          the step's prompt text, or "-" to read it from stdin
  OWN_SESSION_ID  the wizard's own session ID, which is excluded from the search

Scans <COPILOT_HOME or ~/.copilot>/session-state/*/events.jsonl, newest first by the events.jsonl
modified time, and picks the newest session whose user.message content contains the prompt
(whitespace-insensitive). Prints the session ID, the run's final message and every command from
tool.execution_start events. Exits 1 if nothing matches.

The final message is the summary the agent passed to its last task_complete tool call, when it ended
that way (a run can finish with task_complete after a progress update). Otherwise it's the last
non-empty assistant.message content.
"""

import json
import os
import sys
from pathlib import Path
from typing import Optional


def normalise(text: str) -> str:
    return " ".join(text.split())


def read_events(path: Path):
    with path.open(encoding="utf-8", errors="replace") as f:
        for line in f:
            try:
                event = json.loads(line)
            except json.JSONDecodeError:
                continue
            if isinstance(event, dict):
                yield event


def data_of(event: dict) -> dict:
    data = event.get("data")
    return data if isinstance(data, dict) else {}


def matches(path: Path, prompt: str) -> bool:
    for event in read_events(path):
        if event.get("type") == "user.message":
            content = data_of(event).get("content")
            if isinstance(content, str) and prompt in normalise(content):
                return True
    return False


def task_complete_summary(args) -> Optional[str]:
    """The summary text from a task_complete tool call's arguments, if there is one."""
    if isinstance(args, str) and args.strip():
        return args
    if not isinstance(args, dict):
        return None
    for key in ("summary", "message", "result", "text"):
        value = args.get(key)
        if isinstance(value, str) and value.strip():
            return value
    texts = [v for v in args.values() if isinstance(v, str) and v.strip()]
    return "\n\n".join(texts) if texts else None


def report(path: Path) -> None:
    final_message = None
    task_summary = None
    commands = []
    for event in read_events(path):
        kind = event.get("type")
        data = data_of(event)
        if kind == "assistant.message":
            content = data.get("content")
            if isinstance(content, str) and content.strip():
                final_message = content
        elif kind == "tool.execution_start":
            args = data.get("arguments")
            if data.get("toolName") == "task_complete":
                summary = task_complete_summary(args)
                if summary is not None:
                    task_summary = summary
            if isinstance(args, dict) and isinstance(args.get("command"), str):
                commands.append(f"[{data.get('toolName', '?')}] {args['command']}")

    print(f"Session: {path.parent.name}")
    print(f"Transcript: {path}")
    if task_summary is not None:
        print("\n=== Final message (task_complete summary) ===")
        print(task_summary)
    else:
        print("\n=== Final message ===")
        print(final_message if final_message is not None else "(no non-empty assistant message)")
    print(f"\n=== Commands run ({len(commands)}) ===")
    for command in commands:
        print(command)


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if len(sys.argv) != 3:
        print(__doc__.strip(), file=sys.stderr)
        return 2

    prompt_arg, own_id = sys.argv[1], sys.argv[2].strip()
    prompt = normalise(sys.stdin.read() if prompt_arg == "-" else prompt_arg)
    if not prompt:
        print("error: the prompt is empty.", file=sys.stderr)
        return 2

    root = Path(os.environ.get("COPILOT_HOME") or Path.home() / ".copilot") / "session-state"
    files = [p for p in root.glob("*/events.jsonl") if p.parent.name != own_id]
    files.sort(key=lambda p: p.stat().st_mtime, reverse=True)

    for path in files:
        if matches(path, prompt):
            report(path)
            return 0

    print(
        f"No session found: none of {len(files)} transcript(s) under {root} "
        f"(excluding {own_id or 'nothing'}) has a user message containing the prompt. "
        "Ask the participant to paste the agent's final message.",
        file=sys.stderr,
    )
    return 1


if __name__ == "__main__":
    sys.exit(main())
