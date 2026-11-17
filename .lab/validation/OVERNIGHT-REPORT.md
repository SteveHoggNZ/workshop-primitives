# Overnight report: Steps 3–8, 17–18 Nov 2026

Copilot CLI 1.0.112, .NET SDK 10.0.102, macOS. **Every run on `gpt-5.6-luna`, in both arms** (Steps 1–2 were
validated with a `gpt-6.1-sol` Before). 3 runs per arm (per arm of Step 8). Detail: `OVERNIGHT-LOG.md`;
scores: `results.jsonl`; cost and time: `metrics.jsonl`; what wasn't counted and why: `NOTES.md`.

## Verdicts

| Step | Verdict | Before (per run) | After (per run) | Designs | Independent review |
|---|---|---|---|---|---|
| 3 Read-only reviewer agent | **Validated** | 1/4, 1/4, 1/4 | 4/4, 4/4, 4/4 | v1 held | Must-fix: the score didn't show the tool list did the enforcing. Added a control: `tools:` deleted → 2 of 3 edited the doc; with it 0 of 2 (one pushed to `sed -i`). Scorer hardened, re-scored: unchanged |
| 4 TDD skill + phase state | **Validated** | 2/5, 2/5, 2/5 | 5/5, 5/5, 5/5 | v1 held; `lab tdd` hardened after review, Afters re-run (v2) | Must-fix: `first_edit` read `dotnet test` output as an edit. Fixed (commands only). `lab tdd green` now refuses if `src/` changed in red, and counts only failing tests in new/changed files |
| 5 Diagnostics skill (cheaper model) | **Validated** | 2/4, 2/4, 2/4 | 4/4, 4/4, 4/4 | checklist v2; skill v4 | Guard/skill fixes (PowerShell one-line command, a wrong "top row moves" line). 5b (memory) **not built** |
| 6 Permissions, then hooks | **Validated** | 1/4, 1/4, 1/4 | 4/4, 4/4, 4/4 | guard v3 | Guard didn't know VS Code tool names and flagged reads; fixed (13-case table). Interactive `ask` (a person declining) untested |
| 7 Quality gate (stop hook) | **Validated** | 3/5, 3/5, 3/5 | 5/5, 5/5, 5/5 | v3 (3 designs) | v1 counted one lesson twice (review). v2's red test was fixed by 2 of 3 Befores on their own. v3 adds the team's format check |
| 8 Spec-driven development | **Ran, 3 arms; conclusion holds, gate not applicable as written** | one prompt 0, 0, 1 | ask first 7, 7, 7 · Spec Kit 7, 7, 7 | v2 | Must-fix: **I over-answered as the stakeholder in v1** (volunteered the 422 etc.), and the docs contradicted the key. Key fixed, strict answering rule, all 9 runs redone |

Every step's After beat every Before run, and every After used the primitive (skill loaded, agent selected,
hook fired: judged from the transcripts; see `primitive_used` in `metrics.jsonl`). For Steps 3–7 each lesson
item is missed by every Before and passed by every After; baseline items pass in both arms.

**Step 8 against the gate.** The clarifying arms beat the one-shot in every run (7 vs 0–1), means 87.5%.
But the honest finding is that **ask-first equals Spec Kit** on this small item (7, 7, 7 each), at about a
quarter of the credits. That's the step's point ("choose the ceremony"), not a failure, but it means there's no
"primitive wins" in the Step 2 sense for Spec Kit vs asking. Each miss, in both arms, was the one decision that
run didn't ask about.

## Lab improvements (all on `main`, pushed; no force)

Harness (`.lab/scripts/lab.py`, `steps/common.py`):

| Commit | Change |
|---|---|
| `e28015e` | **One build per score** (visible tests and `LabCheckNN` from one TRX; verified identical on 4 clones scored old/new; warm re-score ~5 s); `run` (Copilot CLI with `--usage-output-file`, timing, tool calls), `calibrate`, `metrics`, parallel `prepare`/`score` |
| `6b86da3`, `aaf6a84` | Compact usage (credits, tokens, API s); `run --prompt` / `--flags` for probes |
| `f247f93` | Clones build the lab tool (two first hook calls racing to build it → "hook errored" → a denied `view`) |
| `68e1b6c`, `73c82e1` | `run --append` for multi-turn arms (`--resume` + `--add-dir`); turns without a transcript are refused; **clones get one orphan base commit** (the facilitator history, with the answer key, was readable via `git show`) |
| `e8dabf5`, `2883442`, `211e094`, `c4bcaf7` | `shell_runs()` (joins `read_bash` output to its command), `first_edit` reads commands not output, `calls()` reads a leading section, timing commands recognised |

Lab tool (`tools/Lab`, Windows-safe, no `.sh`/`.ps1`):

| Commit | Command |
|---|---|
| `f1fbf64`, `2883442` | `./lab tdd red\|green\|done\|status` (TRX-based; refuses a compile error, a `src/` change in red, an old failing test; `done` refuses if the red test is gone). Not in `./lab help` |
| `7433b8b` | `./lab warm`: one-off build of the solution and every skill script (packages cached). Mentioned in README, not in `doctor`'s output (that would change Step 0) |
| `e18d445`, `9997180`, `6436381` | `./lab hook guard` (preToolUse: "ask" for writes to `infra/prod/`) |
| `1c14b93`, `368d299`, `47e11c3` | `./lab hook gate` (agentStop: lint vs baseline, format, tests, suppressions, bar files; parallel; fast path), `guard --bar`, `./lab ratchet`, `./lab scenario N` |
| `73c82e1` | `reset` removes Spec Kit's gitignored `.specify/feature.json` |

Base app (neutral subjects from Step 5 on): `/Dashboard` + `TaskStats` (`f1fbf64`), `/Bulk` + search (`d55b2ec`),
`infra/prod/main.tf` (`16446da`), `POST/PATCH /api/tasks` + `docs/backlog.md` (`22a8232`). Each kept `docs/architecture.md`
accurate for what it added, so Step 3's drift key is unchanged; `step01.patch` re-based twice (same +/- lines).

## Steps 0 and 1

Kept as validated: lint `Total: 6 (baseline 6)` with step 0; Step 1 calibration 6/6 and 0/6 (final regression
on `73c82e1`); same prompt, checklist, files. One change you'll see: **Step 0 Part C's line links**. My base
changes had moved `queries.Recent()` into `TaskApi.cs`; I moved it back to `Program.cs` (L37), kept
`TaskService.Recent` at L35, and **corrected the two links**, which had been 2 lines stale since Step 2
(`3eb2870`). Six `Recent(` hits, as the walkthrough says. New nav links (Dashboard, Bulk) appear in the app.

## Branches

`lab/steve` didn't exist when I started (you'd reset it, switched the main checkout to `main` and deleted it). I
created no branches (worktrees only, all removed). Nothing rebased, nothing stale.

## Decisions for you

1. **Answers in the attendee's repo.** Clones delete `.lab/`, but an attendee's repo has `.lab/solutions/*.patch`,
   the hidden checks and the Step 8 answer key in the working tree. A Before that greps the repo could find them.
   Proposal: a root `.ignore` with `.lab/` (ripgrep honours it) and `search.exclude` in `.vscode/settings.json`.
   Not done: it changes the environment Steps 0–1 were validated in.
2. **Commit subjects on `main` name the steps** ("Step 4: tdd skill, …"). An attendee's `git log` shows them.
   Squash before the workshop, or accept it (no run used it).
3. **Step 8 needs a disciplined stakeholder.** The result depends on answering only what's asked (we proved
   it by doing it wrong). The key now has an answering rule; brief facilitators on it.
4. **Step 8's Spec Kit arm is long**: 9–11 min across 5 sessions, implement 3.5–4.9 min (over the 4-min budget
   in 2 of 3 runs). Consider a demo, or a smaller backlog item for the room.
5. **5b (memory growth)** wasn't built. Optional per the curriculum.
6. Claude Code adapters exist only for Step 3; none validated.

## Risks for the demo

- **Windows untested** for everything new: `lab.cmd` (rebuilds on every call, so hooks call
  `dotnet .lab-bin/Lab.dll` on PowerShell), Spec Kit's PowerShell variant, `profile.cs`'s EventPipe self-session.
- **VS Code behaviour unverified** for custom agents' tool aliases (Step 3), hooks (Steps 6–7: does VS Code run
  `.github/hooks/*.json`, `ask` prompts, `agentStop`), and Spec Kit skills as slash commands (Step 8).
- **Interactive `ask`** (Step 6): validated only in `-p`, where `ask` becomes a deny. If an attendee approves,
  the After scores like a Before (the walkthrough says to decline).
- **Machine load.** Under load tonight `dotnet test` went from 0.7 s to 11–50 s; profile.cs from 6 s to 20 s.
- `profile.cs` needs two NuGet packages (`./lab warm` fetches them); a blocked feed means no profiler.

### VS Code checks (each walkthrough has a "VS Code check for Steve", ~3 min)

| Step | Check |
|---|---|
| 3 | `docs-reviewer` in the agent picker; its tools picker shows only read/search; asked to edit, it reports |
| 4 | Step 4 prompt: `SKILL.md` referenced; `./lab tdd red` → `green` (FAIL line) before any edit to `TaskStats.cs` |
| 5 | Run `profile.cs` yourself once; the prompt runs it before editing `TextHelpers.cs` |
| 6 | Hooks enabled; editing `infra/prod/main.tf` shows an approval prompt with the hook's reason; decline it |
| 7 | `./lab scenario 7`; the agent is sent back with "Quality gate failed…" and ends at or below 5 |
| 8 | `/speckit-` slash commands appear; specify writes `[NEEDS CLARIFICATION]` markers |

## Metrics (from `metrics.jsonl`, latest line per run)

| Step | Agent runs counted / not / calibration | Redesigns | Agent wall time (all runs) | AI credits (all runs) | After used the primitive |
|---|---|---|---|---|---|
| 3 | 6 / 7 / 8 | 0 (+ control probes) | 15 min | 10 | 3/3 |
| 4 | 6 / 3 / 6 | 0 (tool hardened, Afters re-run) | 23 min | 12 | 3/3 |
| 5 | 6 / 8 / 6 | 1 checklist + 2 skill wording | 27 min | 17 | 3/3 |
| 6 | 6 / 11 / 6 | 2 guard fixes (+ 7 probes) | 6 min | 6 | 3/3 |
| 7 | 6 / 12 / 8 | 2 | 39 min | 31 | 3/3 (blocked once each) |
| 8 | 9 / 9 / 6 | 1 (stakeholder + key) | 110 min | 163 | 3/3 |

Night totals (Steps 3–8): **89 agent runs** (39 counted, 50 not counted: superseded designs, probes and
controls), **3 h 39 min of agent time, about 238 AI credits**, plus 40 calibration scorings (no agent time),
including the final regression. Calibration columns include the final regression pass. **Slowest and most fragile: Step 8**: multi-turn,
depends on a human stakeholder's discipline (it went wrong once), and its Spec Kit arm alone is ~10 min and
~18 credits a run. **Second: Step 7**, which took three designs to find two independent lessons.

## Attendee timings (rehearsal, fresh clone of `4a01dc6`, quiet machine)

| Step | Slowest command | Time | Agent session times (validation) |
|---|---|---|---|
| Setup | `./lab warm` (one-off) | 11 s | – |
| 0 | `./lab lint` | 3.9 s | – |
| 2 | `try.cs`, first run in a clone | 3.6 s | – |
| 3 | `./lab apply 3` | 0.2 s | Before 51–208 s, After 39–197 s |
| 4 | `./lab tdd green` | 5.0 s | Before 64–95 s, After 167–202 s |
| 5 | `profile.cs`, first run (slow page) | 9.7 s (6.3 s warm; 13–21 s under load) | Before 110–123 s, After 63–76 s (v4) |
| 6 | `./lab hook guard` | 0.1 s | ~20 s each arm |
| 7 | `./lab hook gate` (full check) | 6.8 s | Before ~2.4 min, After ~3 min |
| 8 | `./lab apply 8` | 0.3 s | one prompt 3.4–4.5 min; ask 3.5–4.1 min; Spec Kit 9.3–11.2 min (5 sessions) |

Moved into setup: the profiler's package restore and the first builds (`./lab warm`). Over budget: Step 8's
Spec Kit implement session (3.5–4.9 min) and the Step 4 After (up to 3.4 min, within). Everything else is
inside 10 s per command and 4 min per session.

## Morning summary

Steps 3–7 validated. Step 8 ran cleanly on its second design and shows that asking first gets what Spec Kit
gets here, for a quarter of the cost. Most redesigns came from the independent reviews, which found real
problems each time (the strongest: my own over-answering as Step 8's stakeholder).
