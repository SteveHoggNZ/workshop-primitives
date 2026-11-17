"""Facilitator harness: prepare throwaway clones for a step, score agent runs, list results.

Runs from the repo root with plain Python 3.9+ (no packages):

    python3 .lab/scripts/lab.py prepare STEP PHASE [--model M] [--tag T[,T2...]]   # -> run id(s), clone, transcript path
    python3 .lab/scripts/lab.py prompt RUN_ID                               # -> the prompt(s) to send
    python3 .lab/scripts/lab.py run RUN_ID [RUN_ID ...]                     # -> runs the agent (Copilot CLI), in parallel
    python3 .lab/scripts/lab.py score RUN_ID [RUN_ID ...]                   # -> appends to .lab/validation/results.jsonl
    python3 .lab/scripts/lab.py metrics RUN_ID --design v1 [--counted no --why ...]  # -> appends to metrics.jsonl
    python3 .lab/scripts/lab.py calibrate STEP [STEP ...]                   # -> reference = max, untouched = about 0
    python3 .lab/scripts/lab.py results [STEP]                              # -> table of scored runs
    python3 .lab/scripts/lab.py clean [RUN_ID ...]                          # remove run clones

prepare, run and score take several ids (or tags) and work on them in parallel: clones are independent.
`run` writes RUN_ID.md (transcript), RUN_ID.out (stdout), RUN_ID.usage.json and RUN_ID.run.json (wall time,
usage, tool calls) next to the clone. `score` runs `dotnet test` once with the step's hidden checks copied in
and reads visible tests and hidden checks from the same TRX (filtered by name), and records its wall time.

A clone is made from the committed main branch, the way an attendee would build it: `./lab goto N`
installs the earlier steps (PHASE "after" also installs this step). Then .lab/ is deleted so the agent
can't read guides, examples or answer keys. Clones live OUTSIDE the repo (../.lab-runs by default,
override with LAB_RUNS) so nothing above the clone leaks in.

`run` calls the Copilot CLI as
    copilot -p "<prompt>" --model M [--agent A] --allow-all --trust-folder --share <transcript>
with COPILOT_HOME=<repo>/../.lab-homes/<model> so personal skills/plugins/MCP don't leak in. A step with
several turns (PROMPTS[phase] is a list) gets one fresh session per turn, transcripts RUN_ID-t1.md, -t2.md...,
unless the scorer sets CONTINUE = True (then turns 2+ use --continue).
"""

from __future__ import annotations

import argparse
import datetime as dt
import importlib.util
import json
import os
import shutil
import re
import subprocess
import sys
import threading
import time
import xml.etree.ElementTree as ET
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

LAB = Path(__file__).resolve().parent.parent
ROOT = LAB.parent
RUNS = Path(os.environ.get("LAB_RUNS", ROOT.parent / ".lab-runs"))
HOMES = ROOT.parent / ".lab-homes"
RESULTS = LAB / "validation" / "results.jsonl"
METRICS = LAB / "validation" / "metrics.jsonl"
APPEND_LOCK = threading.Lock()
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
    t0 = time.time()
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
    # Build the lab tool now, as an attendee's repo has it: hooks call ./lab, and two first calls at once would
    # both try to build it (found in Step 6: a view denied with "hook errored").
    sh(["./lab", "help"], run)
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
            "installed": [s for s in installed if s.startswith("lab: step")],
            "prepare_s": round(time.time() - t0)}
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

    def _test_run(self, with_checks):
        """One `dotnet test` with the given hidden check files copied in; {'built', 'rc', 'summary', 'results'}.

        results maps the short test name to its outcome, and full names are kept in 'full' so visible tests and
        hidden checks (classes named LabCheckNN) can be told apart without a second build."""
        out = self.dir / ".lab-score"
        shutil.rmtree(out, ignore_errors=True)
        targets = []
        for name in with_checks:
            target = self.dir / "tests" / "Tasks.App.Tests" / f"{name}.cs"
            shutil.copy(CHECKS / f"{name}.cs", target)
            targets.append(target)
        try:
            cmd = ["dotnet", "test", "-nologo", "-tl:off", "--logger", "trx;LogFileName=t.trx", "--results-directory", str(out)]
            r = sh(cmd, self.dir, check=False, timeout=900)
        finally:
            for t in targets:
                t.unlink(missing_ok=True)
        full = {}
        for trx in out.rglob("*.trx"):
            ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
            for res in ET.parse(trx).getroot().iterfind(".//t:UnitTestResult", ns):
                full[res.get("testName", "")] = res.get("outcome", "")
        lines = (r.stdout + r.stderr).strip().splitlines()
        summary = next((ln.strip() for ln in reversed(lines) if "Passed!" in ln or "Failed!" in ln or "error" in ln), lines[-1] if lines else "")
        shutil.rmtree(out, ignore_errors=True)
        return {"built": bool(full), "rc": r.returncode, "summary": summary[:200], "full": full}

    def _tests(self):
        """The score's single test run, cached. Falls back to a run without the hidden checks only if the
        build failed with them in (a check that no longer compiles against the agent's code must not also
        fail the visible tests)."""
        if getattr(self, "_cache", None) is None:
            names = getattr(self, "check_names", None)
            if names is None:
                names = [p.stem for p in [CHECKS / f"LabCheck{self.meta['step']:02d}.cs"] if p.exists()]
            run = self._test_run(names)
            run["visible_run"] = run
            if names and not run["built"]:
                run["visible_run"] = self._test_run([])
            self._cache = run
        return self._cache

    @staticmethod
    def _is_check(full_name):
        return re.search(r"\.LabCheck\d+\.", full_name) is not None

    def dotnet_test(self, filter_expr=None):
        """The visible tests (everything but LabCheckNN) from the score's one test run.

        Returns {'ok', 'summary', 'results': {short test name: outcome}}. ok means it built and every visible test
        passed. filter_expr is accepted for compatibility: only "FullyQualifiedName!~LabCheck" (or None) is supported."""
        if filter_expr not in (None, "FullyQualifiedName!~LabCheck"):
            raise SystemExit(f"dotnet_test: unsupported filter {filter_expr!r}")
        vr = self._tests()["visible_run"]
        vis = {k: v for k, v in vr["full"].items() if not self._is_check(k)}
        failed = [k for k, v in vis.items() if v not in ("Passed", "NotExecuted")]
        ok = vr["built"] and bool(vis) and not failed
        passed = sum(1 for v in vis.values() if v == "Passed")
        summary = (f"visible tests: {passed} passed, {len(failed)} failed" if vr["built"]
                   else f"build failed: {vr['summary']}")
        return {"ok": ok, "summary": summary[:200], "results": {k.split(".")[-1]: v for k, v in vis.items()}}

    def run_checks(self, name):
        """A hidden check class's results from the score's one test run: {'ok', 'summary', 'results'}."""
        run = self._tests()
        if name not in (getattr(self, "check_names", None) or [f"LabCheck{self.meta['step']:02d}"]):
            raise SystemExit(f"run_checks({name}): set CHECKS in the scorer to include it")
        mine = {k.split(".")[-1]: v for k, v in run["full"].items() if f".{name}." in k}
        ok = bool(mine) and all(v == "Passed" for v in mine.values())
        return {"ok": ok, "summary": run["summary"], "results": mine}

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
    t0 = time.time()
    ev = Evidence(run_id)
    if not ev.transcript.strip():
        raise SystemExit(f"No transcript at {RUNS / (run_id + '.md')}: has the agent run finished?")
    mod = load_scorer(ev.meta["step"])
    if hasattr(mod, "CHECKS"):
        ev.check_names = list(mod.CHECKS)
    checks = mod.score(ev)
    used = mod.primitive_used(ev) if hasattr(mod, "primitive_used") and ev.meta["phase"] != "before" else "n/a"
    result = {**ev.meta, "scored": dt.datetime.now().isoformat(timespec="seconds"),
              "points": sum(1 for _, ok, _ in checks if ok), "max": len(checks),
              "checks": [{"name": n, "ok": ok, "note": note} for n, ok, note in checks],
              "changed": ev.changed()[:40], "scoring_wall_s": round(time.time() - t0), "primitive_used": used}
    with APPEND_LOCK:
        RESULTS.parent.mkdir(parents=True, exist_ok=True)
        with RESULTS.open("a") as f:
            f.write(json.dumps(result) + "\n")
    return result


def _usage(path):
    """The cost fields from a --usage-output-file JSON; 'n/a' when the file or a field is missing."""
    try:
        u = json.loads(path.read_text())
    except (OSError, json.JSONDecodeError):
        return {"usage": "n/a"}
    models = u.get("modelMetrics", {})
    tok = lambda k: sum(m.get("usage", {}).get(k, 0) for m in models.values())
    nano = u.get("totalNanoAiu")
    return {"usage": {"ai_credits": round(nano / 1e9, 2) if isinstance(nano, (int, float)) else "n/a",
                      "premium_requests": u.get("totalPremiumRequestCost", "n/a"),
                      "requests": sum(m.get("requests", {}).get("count", 0) for m in models.values()),
                      "tokens_in": tok("inputTokens"), "tokens_cached": tok("cacheReadTokens"),
                      "tokens_written": tok("cacheWriteTokens"), "tokens_out": tok("outputTokens"),
                      "tokens_reasoning": tok("reasoningTokens"),
                      "api_s": round(u["totalApiDurationMs"] / 1000) if isinstance(u.get("totalApiDurationMs"), (int, float)) else "n/a"}}


def run_agent(run_id, timeout=1200, prompt=None, flags=None):
    """Runs every turn of the run's prompt with the Copilot CLI in its clone; writes RUN_ID.run.json.

    prompt overrides the step's prompt (one turn, the phase's agent) for probe runs, which are never counted."""
    meta = json.loads((RUNS / f"{run_id}.json").read_text())
    mod = load_scorer(meta["step"])
    turns = prompt_for(run_id)
    if prompt:
        turns = [{"agent": turns[0]["agent"], "prompt": prompt}]
    home = HOMES / meta["model"]
    if not home.exists():
        raise SystemExit(f"No Copilot home {home}: sign in there first")
    record = {"run_id": run_id, "turns": [], **({"probe_prompt": prompt} if prompt else {}), **({"probe_flags": flags} if flags else {})}
    for i, turn in enumerate(turns, 1):
        suffix = f"-t{i}" if len(turns) > 1 else ""
        transcript = RUNS / f"{run_id}{suffix}.md"
        usage_file = RUNS / f"{run_id}{suffix}.usage.json"
        cmd = ["copilot", "-p", turn["prompt"], "--model", meta["model"], "--allow-all", "--trust-folder",
               "--no-ask-user", "--share", str(transcript), "--usage-output-file", str(usage_file)]
        if turn["agent"]:
            cmd += ["--agent", turn["agent"]]
        if flags:
            cmd += flags  # probe runs only (e.g. --deny-tool); never counted
        if i > 1 and getattr(mod, "CONTINUE", False):
            cmd += ["--continue"]
        started = dt.datetime.now().isoformat(timespec="seconds")
        t0 = time.time()
        try:
            r = subprocess.run(cmd, cwd=RUNS / run_id, capture_output=True, text=True, timeout=timeout,
                               env={**os.environ, "COPILOT_HOME": str(home)})
            rc, out = r.returncode, r.stdout + ("\n[stderr]\n" + r.stderr if r.stderr.strip() else "")
        except subprocess.TimeoutExpired as e:
            rc, out = "timeout", (e.stdout or b"").decode(errors="replace") if isinstance(e.stdout, bytes) else (e.stdout or "")
        wall = round(time.time() - t0)
        (RUNS / f"{run_id}{suffix}.out").write_text(out)
        text = transcript.read_text(errors="replace") if transcript.exists() else ""
        tool_calls = len(re.findall(r"\n### `[^`]+`", text))
        record["turns"].append({"turn": i, "agent": turn["agent"], "started": started, "agent_wall_s": wall, "rc": rc,
                                "tool_calls": tool_calls, **_usage(usage_file)})
    record["agent_wall_s"] = sum(t["agent_wall_s"] for t in record["turns"])
    record["tool_calls"] = sum(t["tool_calls"] for t in record["turns"])
    (RUNS / f"{run_id}.run.json").write_text(json.dumps(record, indent=2))
    return record


def metrics(run_id, design, counted, why, primitive):
    """Appends one per-run line to metrics.jsonl from RUN_ID.json, .run.json and its latest score."""
    meta = json.loads((RUNS / f"{run_id}.json").read_text())
    runp = RUNS / f"{run_id}.run.json"
    run = json.loads(runp.read_text()) if runp.exists() else {}
    scored = [json.loads(l) for l in RESULTS.read_text().splitlines() if l.strip() and json.loads(l)["id"] == run_id] if RESULTS.exists() else []
    last = scored[-1] if scored else {}
    n = len(run.get("turns", []))
    usage = [_usage(RUNS / f"{run_id}{'-t' + str(i) if n > 1 else ''}.usage.json")["usage"] for i in range(1, n + 1)]
    rec = {"kind": "run", "run_id": run_id, "step": meta["step"], "arm": meta["phase"], "model": meta["model"],
           "design": design, "prepared": meta["created"], "prepare_s": meta.get("prepare_s", "n/a"),
           "agent_started": run["turns"][0]["started"] if run.get("turns") else "n/a",
           "agent_wall_s": run.get("agent_wall_s", "n/a"), "turns": len(run.get("turns", [])) or "n/a",
           "usage": usage if usage else "n/a", "tool_calls": run.get("tool_calls", "n/a"),
           "primitive_used": primitive or last.get("primitive_used", "n/a"),
           "score": last.get("points", "n/a"), "max": last.get("max", "n/a"), "scored": last.get("scored", "n/a"),
           "scoring_wall_s": last.get("scoring_wall_s", "n/a"), "counted": counted, "why": why}
    with APPEND_LOCK:
        with METRICS.open("a") as f:
            f.write(json.dumps(rec) + "\n")
    return rec


def calibrate(step):
    """Scores the reference solution (an after clone + solutions/stepNN.patch + the scorer's CALIB["ref"] stub
    transcript) and an untouched before clone (CALIB["base"] stub). No agent runs. Returns both results."""
    mod = load_scorer(step)
    stubs = getattr(mod, "CALIB", {})
    stamp = dt.datetime.now().strftime("%m%d%H%M%S")
    ref, base = parallel(lambda ph: prepare(step, ph[0], "calib", f"{ph[1]}-{stamp}"), [("after", "ref"), ("before", "base")])
    patch = LAB / "solutions" / f"step{step:02d}.patch"
    if patch.exists():
        sh(["git", "apply", str(patch)], Path(ref["clone"]))
    if hasattr(mod, "calib_ref"):
        mod.calib_ref(Path(ref["clone"]))
    for r, key in ((ref, "ref"), (base, "base")):
        Path(r["transcript"]).write_text(stubs.get(key, "### Copilot\n\nNothing done.\n"))
    return parallel(score, [ref["id"], base["id"]])


def parallel(fn, items):
    with ThreadPoolExecutor(max_workers=max(1, len(items))) as pool:
        return list(pool.map(fn, items))


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
    p.add_argument("--model", default="gpt-5.6-luna"); p.add_argument("--tag", default="", help="one tag, or several comma-separated (prepared in parallel)")
    s = sub.add_parser("score"); s.add_argument("run_ids", nargs="+")
    rn = sub.add_parser("run"); rn.add_argument("run_ids", nargs="+"); rn.add_argument("--timeout", type=int, default=1200)
    rn.add_argument("--prompt", default=None, help="probe: replace the step's prompt (never counted)")
    rn.add_argument("--flags", default=None, help="probe: extra copilot arguments, one string, split on spaces (never counted)")
    m = sub.add_parser("metrics"); m.add_argument("run_ids", nargs="+"); m.add_argument("--design", required=True)
    m.add_argument("--counted", default="yes"); m.add_argument("--why", default=""); m.add_argument("--primitive", default="")
    cb = sub.add_parser("calibrate"); cb.add_argument("steps", type=int, nargs="+")
    pr = sub.add_parser("prompt"); pr.add_argument("run_id")
    rs = sub.add_parser("results"); rs.add_argument("step", type=int, nargs="?")
    c = sub.add_parser("clean"); c.add_argument("ids", nargs="*")
    a = ap.parse_args()
    if a.cmd == "prepare":
        tags = a.tag.split(",") if "," in a.tag else [a.tag]
        out = parallel(lambda t: prepare(a.step, a.phase, a.model, t), tags)
        print(json.dumps(out[0] if len(out) == 1 else [{k: o[k] for k in ("id", "clone", "prepare_s")} for o in out], indent=2))
    elif a.cmd == "run":
        for r in parallel(lambda i: run_agent(i, a.timeout, a.prompt, a.flags.split() if a.flags else None), a.run_ids):
            print(f"{r['run_id']}: {r['agent_wall_s']} s, {r['tool_calls']} tool calls, rc {[t['rc'] for t in r['turns']]}")
    elif a.cmd == "score":
        for r in parallel(score, a.run_ids):
            print(f"{r['id']}: {r['points']}/{r['max']}  ({r['scoring_wall_s']} s, primitive used: {r['primitive_used']})")
            for ch in r["checks"]:
                print(f"  [{'x' if ch['ok'] else ' '}] {ch['name']}  {ch['note']}")
    elif a.cmd == "metrics":
        for i in a.run_ids:
            r = metrics(i, a.design, a.counted, a.why, a.primitive)
            print(f"{i}: {r['score']}/{r['max']} counted={r['counted']}")
    elif a.cmd == "calibrate":
        for res in parallel(calibrate, a.steps):
            for r in res:
                marks = "".join("x" if c["ok"] else "." for c in r["checks"])
                print(f"{r['id']}: {r['points']}/{r['max']}  [{marks}]  ({r['scoring_wall_s']} s)")
    elif a.cmd == "prompt":
        print(json.dumps(prompt_for(a.run_id), indent=2))
    elif a.cmd == "results":
        results(a.step)
    elif a.cmd == "clean":
        clean(a.ids)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
