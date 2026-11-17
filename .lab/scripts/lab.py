"""Facilitator harness: prepare throwaway clones for a step, score agent runs, list results.

Runs from the repo root with plain Python 3.9+ (no packages):

    python3 .lab/scripts/lab.py prepare STEP PHASE [--model M] [--tag T]   # -> run id, clone, transcript path
    python3 .lab/scripts/lab.py prompt RUN_ID                               # -> the prompt(s) to send
    python3 .lab/scripts/lab.py score RUN_ID                                # -> appends to .lab/validation/results.jsonl
    python3 .lab/scripts/lab.py results [STEP]                              # -> table of scored runs
    python3 .lab/scripts/lab.py clean [RUN_ID ...]                          # remove run clones

A clone is made from the committed main branch, the way an attendee would build it: `./lab goto N`
installs the earlier steps (PHASE "after" also installs this step). Then .lab/ is deleted so the agent
can't read guides, examples or answer keys. Clones live OUTSIDE the repo (../.lab-runs by default,
override with LAB_RUNS) so nothing above the clone leaks in.

The agent is run separately with the Copilot CLI, for example via the copilot MCP wrapper:
    copilot -p "<prompt>" --model M --allow-all --trust-folder --share <transcript>
with COPILOT_HOME=<repo>/../.lab-homes/<model> so personal skills/plugins/MCP don't leak in.
"""

from __future__ import annotations

import argparse
import datetime as dt
import importlib.util
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

LAB = Path(__file__).resolve().parent.parent
ROOT = LAB.parent
RUNS = Path(os.environ.get("LAB_RUNS", ROOT.parent / ".lab-runs"))
HOMES = ROOT.parent / ".lab-homes"
RESULTS = LAB / "validation" / "results.jsonl"
STEPS = LAB / "scripts" / "steps"
CHECKS = LAB / "scripts" / "checks"
GIT_ID = ["-c", "user.name=Lab Harness", "-c", "user.email=lab@example.invalid"]
DOTNET_ENV = {"DOTNET_NOLOGO": "1", "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "MSBUILDDISABLENODEREUSE": "1",
              "DOTNET_CLI_UI_LANGUAGE": "en"}


def sh(cmd, cwd, check=True, env=None, timeout=900):
    r = subprocess.run(cmd, cwd=cwd, check=False, capture_output=True, text=True,
                       env={**os.environ, **DOTNET_ENV, **(env or {})}, timeout=timeout)
    if check and r.returncode != 0:
        raise SystemExit(f"FAILED ({r.returncode}): {' '.join(map(str, cmd))}\n{r.stdout}\n{r.stderr}")
    return r


def load_scorer(step):
    path = STEPS / f"step{step:02d}.py"
    if str(STEPS) not in sys.path:
        sys.path.insert(0, str(STEPS))
    spec = importlib.util.spec_from_file_location(f"step{step:02d}", path)
    if spec is None or spec.loader is None:
        raise SystemExit(f"No scorer {path}")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def prepare(step, phase, model, tag):
    stamp = dt.datetime.now().strftime("%m%d-%H%M%S")
    run_id = f"s{step:02d}-{phase}-{model}-{tag or stamp}".replace("/", "_")
    run = RUNS / run_id
    if run.exists():
        raise SystemExit(f"{run} exists; use another --tag or clean it")
    RUNS.mkdir(parents=True, exist_ok=True)
    sh(["git", "clone", "-q", "--no-hardlinks", "--branch", "main", str(ROOT), str(run)], ROOT)
    sh(["git", "remote", "remove", "origin"], run)
    upto = step + 1 if phase == "after" else step
    if upto > 0:
        sh(["./lab", "goto", str(upto)], run, env={"GIT_AUTHOR_NAME": "Lab Harness", "GIT_AUTHOR_EMAIL": "lab@example.invalid",
                                                    "GIT_COMMITTER_NAME": "Lab Harness", "GIT_COMMITTER_EMAIL": "lab@example.invalid"})
    shutil.rmtree(run / ".lab")
    shutil.rmtree(run / ".lab-bin", ignore_errors=True)
    sh(["git", *GIT_ID, "commit", "-qam", "lab: remove facilitator files"], run)
    mod = load_scorer(step)
    if hasattr(mod, "setup"):
        mod.setup(run)
        sh(["git", "add", "-A"], run)
        sh(["git", *GIT_ID, "commit", "-qm", f"lab: step{step:02d} scenario setup", "--allow-empty"], run)
    # Restore only. Don't build: an incremental `dotnet build` prints no warnings for code that's already
    # compiled, which hides the lint findings from an agent that checks with a build (found in Step 0 r1).
    sh(["dotnet", "restore", "-nologo", "-v:q", "-tl:off"], run, check=False)
    base = sh(["git", "rev-parse", "HEAD"], run).stdout.strip()
    installed = sh(["git", "log", "--format=%s"], run).stdout.split("\n")
    meta = {"id": run_id, "step": step, "phase": phase, "model": model, "base": base,
            "created": dt.datetime.now().isoformat(timespec="seconds"),
            "installed": [s for s in installed if s.startswith("lab: step")]}
    (RUNS / f"{run_id}.json").write_text(json.dumps(meta, indent=2))
    HOMES.mkdir(parents=True, exist_ok=True)
    return {**meta, "clone": str(run), "transcript": str(RUNS / f"{run_id}.md"),
            "copilot_home": str(HOMES / model), "prompt": prompt_for(run_id)}


class Evidence:
    """What happened in a run: files changed, tests, lint, transcript."""

    def __init__(self, run_id):
        self.id = run_id
        self.dir = RUNS / run_id
        self.meta = json.loads((RUNS / f"{run_id}.json").read_text())
        parts = sorted(RUNS.glob(f"{run_id}-t*.md")) or [RUNS / f"{run_id}.md"]
        self.transcript = "\n".join(p.read_text(errors="replace") for p in parts if p.exists())

    def git(self, *args):
        return sh(["git", *args], self.dir, check=False).stdout

    def changed(self):
        names = set(self.git("diff", "--name-only", self.meta["base"]).split())
        names |= set(self.git("ls-files", "--others", "--exclude-standard").split())
        return sorted(n for n in names if n)

    def diff(self, *paths):
        sh(["git", "add", "-N", "."], self.dir, check=False)
        return self.git("diff", self.meta["base"], "--", *paths)

    def read(self, rel):
        p = self.dir / rel
        return p.read_text(errors="replace") if p.exists() else ""

    def dotnet_test(self, filter_expr=None):
        """Runs the tests; returns {'ok', 'summary', 'results': {test name: outcome}}."""
        out = self.dir / ".lab-score"
        shutil.rmtree(out, ignore_errors=True)
        cmd = ["dotnet", "test", "-nologo", "-tl:off", "--logger", "trx;LogFileName=t.trx", "--results-directory", str(out)]
        if filter_expr:
            cmd += ["--filter", filter_expr]
        r = sh(cmd, self.dir, check=False, timeout=900)
        results = {}
        for trx in out.rglob("*.trx"):
            import xml.etree.ElementTree as ET
            ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
            for res in ET.parse(trx).getroot().iterfind(".//t:UnitTestResult", ns):
                results[res.get("testName", "").split(".")[-1]] = res.get("outcome", "")
        lines = (r.stdout + r.stderr).strip().splitlines()
        summary = next((ln.strip() for ln in reversed(lines) if "Passed!" in ln or "Failed!" in ln or "error" in ln), lines[-1] if lines else "")
        shutil.rmtree(out, ignore_errors=True)
        return {"ok": r.returncode == 0, "summary": summary[:200], "results": results}

    def run_checks(self, name):
        """Copies a hidden check file into the test project, runs only those tests, removes it."""
        target = self.dir / "tests" / "Tasks.App.Tests" / f"{name}.cs"
        shutil.copy(CHECKS / f"{name}.cs", target)
        try:
            return self.dotnet_test(f"FullyQualifiedName~{name}")
        finally:
            target.unlink(missing_ok=True)

    def lint(self):
        r = sh(["./lab", "lint", "--json"], self.dir, check=False)
        try:
            return json.loads(r.stdout.strip().splitlines()[-1])
        except (json.JSONDecodeError, IndexError):
            return {"error": (r.stdout + r.stderr)[-500:]}

    def ran(self, needle):
        return needle in self.transcript


def prompt_for(run_id):
    meta = json.loads((RUNS / f"{run_id}.json").read_text())
    mod = load_scorer(meta["step"])
    agent = getattr(mod, "AGENT", {}).get(meta["phase"])
    p = mod.PROMPTS[meta["phase"]]
    turns = p if isinstance(p, list) else [(agent, p)]
    return [{"agent": a, "prompt": t} for a, t in turns]


def score(run_id):
    ev = Evidence(run_id)
    if not ev.transcript.strip():
        raise SystemExit(f"No transcript at {RUNS / (run_id + '.md')}: has the agent run finished?")
    mod = load_scorer(ev.meta["step"])
    checks = mod.score(ev)
    result = {**ev.meta, "scored": dt.datetime.now().isoformat(timespec="seconds"),
              "points": sum(1 for _, ok, _ in checks if ok), "max": len(checks),
              "checks": [{"name": n, "ok": ok, "note": note} for n, ok, note in checks],
              "changed": ev.changed()[:40]}
    RESULTS.parent.mkdir(parents=True, exist_ok=True)
    with RESULTS.open("a") as f:
        f.write(json.dumps(result) + "\n")
    return result


def results(step):
    if not RESULTS.exists():
        print("No results yet.")
        return
    rows = [json.loads(l) for l in RESULTS.read_text().splitlines() if l.strip()]
    for r in rows:
        if step is not None and r["step"] != step:
            continue
        marks = "".join("x" if c["ok"] else "." for c in r["checks"])
        print(f"s{r['step']:02d} {r['phase']:<6} {r['model']:<14} {r['points']}/{r['max']}  [{marks}]  {r['id']}")


def clean(ids):
    targets = [RUNS / i for i in ids] if ids else [p for p in RUNS.iterdir() if p.is_dir()]
    for t in targets:
        shutil.rmtree(t, ignore_errors=True)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("prepare"); p.add_argument("step", type=int); p.add_argument("phase", choices=["before", "ask", "after"])
    p.add_argument("--model", default="gpt-5.6-luna"); p.add_argument("--tag", default="")
    s = sub.add_parser("score"); s.add_argument("run_id")
    pr = sub.add_parser("prompt"); pr.add_argument("run_id")
    rs = sub.add_parser("results"); rs.add_argument("step", type=int, nargs="?")
    c = sub.add_parser("clean"); c.add_argument("ids", nargs="*")
    a = ap.parse_args()
    if a.cmd == "prepare":
        print(json.dumps(prepare(a.step, a.phase, a.model, a.tag), indent=2))
    elif a.cmd == "score":
        r = score(a.run_id)
        print(f"{r['id']}: {r['points']}/{r['max']}")
        for ch in r["checks"]:
            print(f"  [{'x' if ch['ok'] else ' '}] {ch['name']}  {ch['note']}")
    elif a.cmd == "prompt":
        print(json.dumps(prompt_for(a.run_id), indent=2))
    elif a.cmd == "results":
        results(a.step)
    elif a.cmd == "clean":
        clean(a.ids)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
