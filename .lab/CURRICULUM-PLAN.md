# Curriculum plan: which repos to use, what each is for, and what our lab keeps

Status: **proposal for review.** Written 2026-11-17 after reviewing:

- the official **Tailspin Toys** workshop (VS Code harness on the awesome-copilot Learning Hub), plus the local `third-party-examples/tailspin-toys` template
- `third-party-examples/copilot-customizations-workshop` (60-minute breakout on instructions, agents, skills and hooks)
- `third-party-examples/copilot-cli-deep-dive-workshop` (half-day, 13 modules on Copilot CLI)
- `nexus-platform/modules/training` (Nexus guides and courses, 70+ learning units)
- our Python lab `_old/workshop-primitives-python` and `.lab/PORT-PLAN.md`

This file lives in `.lab/` because it names step designs and planted problems.

---

## 1. The short version

| Repo | Role | Goal for the learner | We maintain it? |
|---|---|---|---|
| **Tailspin Toys, VS Code harness** (official) | **Pre-work: the basics** | Comfortable driving agent mode; has seen custom instructions, path-scoped instructions, Playwright MCP, a custom agent, steering a session, reviewing a PR | No: point people at it |
| **Nexus training** | **The why: concepts and mental models** | Understands *why* each practice works: the primitives map, trust evidence, self-report vs outside checks, spec-driven development, agent tools, brownfield | Yes (separately); the lab links to it |
| **`workshop-primitives`** (ours, new, .NET) | **The proof: choose, apply and measure primitives** | Can pick the right primitive for a gap, prove it changed agent behaviour, enforce what must always hold, and clarify before building | Yes: the main lab |
| `_old/workshop-primitives-python` | Reference only | none (attendees never see it) | Freeze; it's the validated source we port from |
| copilot-customizations-workshop, copilot-cli-deep-dive-workshop | Inspiration only | none: don't send attendees, to avoid overlap | No |

The thread through all of it: **Tailspin shows what each feature does. Nexus explains why it works. Our lab proves which one closes which gap, and that "asking" isn't the same as "enforcing".**

---

## 2. Who covers what today

✅ taught hands-on · ◐ mentioned or light · — absent

| Topic | Tailspin VS Code | Customizations wkshp | CLI deep-dive | Nexus training | Python lab |
|---|---|---|---|---|---|
| Agent mode, steering, PR review | ✅ | ◐ | ✅ (modes, sessions) | ◐ (own-the-loop) | — |
| Repo instructions | ✅ (adds a standard, re-runs) | ✅ (unguided vs guided) | ✅ (AGENTS.md, nested) | ✅ (rules-and-state) | ✅ step 1 |
| Path-scoped instructions | ✅ | ◐ bonus | ✅ | — | ✅ step 2 |
| Prompt files | — | ◐ bonus | — | — | ◐ step 3 |
| Skills | — (other Tailspin harnesses have `quality-checks`) | ✅ | ✅ (auto-selection) | ✅ (say-when-not-to) | ✅ steps 3, 6, 8–9 |
| Custom agents (persona) | ✅ (accessibility agent) | ✅ (+ handoffs) | ✅ (+ subagents) | ◐ (specialisation) | ✅ step 4 |
| Agent with restricted tools as enforcement | — | ◐ bonus reviewer | ✅ (tool restrictions) | ✅ (limits-in-code) | ✅ step 4 |
| MCP / browser | ✅ (Playwright) | ◐ | ✅ | — | ✅ step 7 |
| Plugins | — | — | ✅ | ◐ (promotion path) | ✅ step 5 |
| Hooks as observability (run tests) | — | ✅ | ✅ | — | ✅ step 10 |
| Hooks as policy (deny/ask) and **their limits** | — | ◐ stretch | ✅ (fail-closed errors, **fail-open timeouts**) | ✅ (limits-in-code) | ✅ step 10 |
| Permissions vs hooks | — | — | ✅ (allow/deny, availability vs permission, sandbox) | — | ◐ (said, not exercised) |
| Stop gate / "self-report isn't a gate" | — | ◐ (Stop hook runs tests) | ◐ (`agentStop` block) | ✅ (self-report) | ✅ step 11 |
| Process evidence (test-first, measure-first) | — | — | — | ✅ (deterministic checks) | ✅ steps 6, 8, 9 |
| Cheaper model with the right primitive | — | — | — | ✅ (subagent patterns) | ✅ steps 8–9 |
| **Spec-driven development** | — | — | — | ✅ (9 units, concepts only) | ✅ step 12 |
| Agent CLI ("repeats become commands", limits in the verb) | — | — | — | ✅ (agent-tools guide) | — |
| **Measuring before vs after** | ◐ ("watch the trend") | ◐ (one baseline) | — | ✅ (advice has a model version) | ✅ every step |
| Brownfield repo with planted gaps | ◐ (real app, no plants) | — (greenfield) | — | ✅ (brownfield) | ✅ |
| Validated across models, stamped | — | — | ◐ (validated against CLI version) | ✅ | ✅ |

**What our lab alone covers hands-on:**

- measured before/after
- durable gap types
- enforcement *and its limits*
- process evidence
- the cheaper-model lesson
- spec-driven development

**Agent CLIs** are taught in Nexus, but no lab does them hands-on.

---

## 3. What to keep, merge or drop from the Python lab

### Keep as is (the lab's value)

| What | Why it stays |
|---|---|
| **The method:** one variable, fixed prompt, fixed checklist, fresh session, isolated config | No other workshop measures. It's what makes our lab more than a feature tour |
| **The four durable gap types** (information, capability, enforcement, ambiguity) | It's the decision rule for choosing a primitive, and it keeps steps valid as models improve |
| **Instructions scored on decisions the code can't show** (zone label, docs table, changelog, report the lint total) | Tailspin teaches *that* instructions work; we teach *what belongs in them* |
| **Read-only agent: the tool list does the enforcing, not a polite request** | The smallest example of "asking vs enforcing" |
| **TDD skill + phase state that refuses green without a real red** | Unique: it turns a habit into evidence, and the hooks step builds on it |
| **Measure-first diagnostics skill + cheaper-model framing** | Unique, and ties to the Nexus cost lessons |
| **Hooks: deny vs ask, and the limits seen live** (shell workarounds) | No one else shows agents routing around a guard |
| **Stop gate with a ratcheted baseline** | The "self-report isn't a gate" lesson, made physical |
| **Spec-driven development: one-shot vs ask-first vs Spec Kit + constitution** | **The headline gap in every other workshop.** Our three-arm comparison is also the honest version: "ask first" gets most of the gain for a small change |
| Validation harness, model/CLI stamps, ≥3 runs per arm, the drift policy | It's how we keep the lab honest; it's also the Nexus "advice has a model version" lesson in practice |
| `lab-wizard` coach that never does the work; facilitator answer keys; `goto`/reset tooling | Better than copy-a-snapshot recovery |
| The FRICTION.md fixes | Already listed in PORT-PLAN §12 |

### Merge (same lesson, less time)

| From | Into | Why |
|---|---|---|
| Step 2 scoped rules | **Step 1** as part B: the step's second rule goes in a path-scoped file | Tailspin already teaches the mechanics; we keep only the "context is a budget" point |
| Step 3 prompt file | Step 2 (skill) as a "Try" | It's a one-minute contrast: human-invoked vs model-invoked |
| Step 9 memory growth | Step 5 (diagnostics) as optional part B | Same skill, same lesson ("grow it, don't duplicate it"); one validated core plus an extension |
| Step 11 LSP demo | Drop to a one-line aside | The Python runs found agents rarely use it unprompted, and a C# LSP setup is a spike in its own right |

### Demote to optional or drop

| What | Decision | Why |
|---|---|---|
| Step 5 plugin + marketplace | **Optional appendix** | Corporate marketplace and plugin policies vary; the validator was hand-scored (2 pts); the Nexus primitives map already teaches the promotion path |
| Step 7 debugger agent + Playwright MCP | **Optional**, for groups that skipped Tailspin | Tailspin exercise 3 does Playwright MCP. Keep our planted `board.js` bug for the optional "reproduce before you fix" variant |
| Claude Code / Codex adapters | Appendix note: "one canonical file, thin pointers, watch double-loading" | Corporate audience is Copilot-first; adapters double the validation work |
| Python-only material: uv, Ruff/Pyright, `phase.py` | Replaced in the port | Ported per PORT-PLAN |
| Stray files: `ai_development_hierarchy_examples.md`, `tmp.md` | Don't carry over | Not lab material (FRICTION S1) |
| `best-practices.md` §6 (three TDD agents with handoffs) | Rewrite | Out of date: replaced by one `tdd` skill (build log item 8) |

### Add (new, small)

| What | Source of the idea | Where |
|---|---|---|
| **Permissions first, then hooks:** run the same "don't touch `infra/prod`" rule as a tool deny / excluded tool, then as a hook, and compare what each can express | CLI deep-dive module 4 | Hooks step, part A |
| **Hook failure semantics:** command errors deny, but **timeouts let the action through** (fail-open) | CLI deep-dive module 9 (verify on the CLI version we validate against) | Hooks step, "limits" section |
| **Observability vs policy hooks** (non-blocking vs blocking) | Customizations workshop lesson 4 | Hooks step, one paragraph |
| **Agent CLI with limits in the verb** (`lab orient`, `infra plan` that refuses `apply`) | Nexus agent-tools guide (no lab teaches it) | Optional track, paired with the IaC examples |
| **A "which primitive when" card** at the end, built from the gap types and our measured results | Customizations workshop bonus 5 has a decision matrix; ours is evidence-backed | Wrap-up handout |
| **Pacing checkpoints + failure-mode tables** in the facilitator guide | Customizations workshop `FACILITATOR.md` | Facilitator kit |
| **Corporate feed pass-through in the dev container** (`RestoreSources`, `NPM_CONFIG_REGISTRY`) | CLI deep-dive `.devcontainer` | PORT-PLAN §3 (locked-down environments) |
| **A content-drift checker agent** that probes `copilot --version` and flags stale claims | CLI deep-dive's `content-accuracy-checker` / `workshop-runner` agents | Facilitator tooling (keep our version stamps; they deliberately go version-free) |

### Not borrowing

- Greenfield "build from an empty folder" (customizations workshop). Our point is brownfield.
- Handoffs between agents. Interesting, but a new primitive adds time. Spec Kit's own phase handoffs show it in passing in the spec-driven step.
- Fleet, autopilot, sessions, CI usage, configuration deep-dives (CLI deep-dive). That's CLI fluency, not primitive choice; point to it as further reading at most.
- Tailspin's agentic workflows, canvases and Agent Merge (other harnesses). Out of scope.

---

## 4. Proposed step list for `workshop-primitives`

Twelve steps become **8 core steps + optional tracks**. Each core step is validated with the full gate set (PORT-PLAN §9).

| # | Session | Step | Gap | From Python | Nexus reading (before/after) |
|---|---|---|---|---|---|
| 0 | Setup | Fair test (`lab doctor`, fresh sessions, personal config off) + **warm-up loop with a linter**: ask a new session what's wrong with Recent, turn the lint rules on, ask again. Before = judgement from reading; After = the six diagnostics. Teaches the loop itself on something visual | none (practice run; deterministic check) | new (Steve's idea, 17 Nov) | `how-models-work.trained-to-pass`, `proof-and-reuse.self-report`, `advice-has-a-model-version.advice-has-a-model-version` |
| 1 | **A. Tell it what it can't know** | Project instructions + one scoped rule (decisions the code can't show; context budget) | Information | 1 + 2 | `foundations.ai-primitives`, `agent-tools.rules-and-state`, `how-models-work.trained-to-pass` |
| 2 | A | Skill with a tested script; description says when *not* to use it; prompt file as Try | Information | 3 | `advice-has-a-model-version.say-when-not-to`, `agent-tools.repeats-become-commands` |
| 3 | A | Read-only reviewer: the tool list does the enforcing | Enforcement | 4 | `agent-tools.limits-in-code` |
| 4 | **B. Evidence, not claims** | TDD skill + phase state (test-first as evidence) | Enforcement / process | 6 | `trust-evidence.deterministic-checks`, `trust-evidence.compound-errors` |
| 5 | B | Diagnostics skill: measure first; cheaper model; (5b optional: grow it for memory growth) | Capability | 8 + 9 | `spend-context.subagent-patterns`, `advice-has-a-model-version.models-learn-the-harness` |
| 6 | **C. Rules, not requests** | Permissions, then hooks: deny vs ask; what each can express; limits (workarounds, fail-open timeouts) | Enforcement | 10 + additions | `agent-tools.limits-in-code` |
| 7 | C | Quality gate: stop hook + ratcheted baseline | Enforcement | 11 | `proof-and-reuse.self-report` |
| 8 | **D. Clarify before you build** | Spec-driven development: one-shot vs ask-first vs Spec Kit + constitution; fresh session per phase | Ambiguity | 12 | `spec-driven-development.gaps-become-guesses`, `.choose-the-ceremony`, `.review-the-spec`, `.when-it-pays`; `agree-the-outcome.align-on-intent`; `own-the-loop.one-job-per-context`; `spend-context.rpi` |

**Optional tracks** (each validated only if we ship it):

| Track | Contents | Why optional |
|---|---|---|
| **I. Infrastructure (Terraform / Bicep)** | I1 scoped IaC rules; I2 an agent-CLI `infra` verb with limits in code (plan only, refuses prod), plus a lint hook; I3 IaC lint in the quality gate | Needs tools a locked-down machine may not have; also where corporate attendees feel enforcement risk most |
| **P. Package it** | Plugin + local marketplace (old step 5) | Policy-dependent |
| **B. Browser** | Debugger agent + Playwright MCP on the planted `board.js` bug (old step 7) | Covered by Tailspin pre-work |

The sessions follow the same arc as the Python lab: soft guidance → evidence → hard enforcement → ambiguity. Each session title is a Nexus idea, so the lab and the training share vocabulary.

**Validation effect:** 8 core steps instead of 12 brings the PORT-PLAN estimate from about 100 agent runs to about **65–70** for the core.

---

## 5. How the three resources fit together for an attendee

```
Before the day      Tailspin Toys (VS Code harness), about 1–2 h self-paced
                    + Nexus: ai-primitives, trust-evidence, spec-driven-development intro
On the day          workshop-primitives, sessions A–D (two half-days, or one long day)
                    each step links its Nexus reading; the facilitator uses the Nexus visuals for the "why"
After               Nexus courses: Brownfield, Agent patterns, AI FinOps
                    + the "which primitive when" card + the attendee's own repo
```

- **Nexus already has a "Brownfield" course** (brownfield, agent CLI ×3, deterministic checks, choose the ceremony, home repo). Our lab is effectively its hands-on practical, so position it that way.
- **Corporate caveat for Tailspin:** it assumes a template repo and Codespaces; a local dev container works too. Sites that block Codespaces need the dev-container path in the pre-work email.
- **A community .NET variant of Tailspin** exists, for the CLI harness (`mattleibow/tailspin-toys-workshop-dotnet`). I haven't reviewed it. It's worth a look if attendees prefer pre-work in C#, but the official VS Code harness is the safer default.

---

## 6. Changes this makes to PORT-PLAN.md (once agreed)

- §6c and §9–10: renumber to the 8 core steps; P3–P5 phases regroup as sessions A–D; old steps 5 and 7 move to optional tracks.
- §8 IaC track: I2 becomes the agent-CLI `infra` verb with limits in code (instead of only a lint hook).
- §11 spikes: add "hook timeout behaviour (fail-open?) on the validated CLI" and "permission deny vs hook for `infra/prod`", both run on macOS and Windows.
- §12: add the facilitator-kit borrowings (pacing checkpoints, failure-mode tables).
- Open question 5 (Claude/Codex adapters): answered as an appendix only, unless you prefer otherwise.

---

## 7. Decisions for you

1. **Accept 8 core steps + optional tracks,** or keep all 12 as core?
2. **Plugins:** optional appendix (recommended), or core?
3. **Agent-CLI lesson:** in the IaC track (recommended), or a core step 9?
4. **Pre-work:** make Tailspin VS Code *required* (lets us skip basics), or *recommended* (we keep a 5-minute recap in step 1)?
5. **Python repo:** freeze as reference (recommended), or keep maintaining both?

---

## 8. Open questions from outside research (18 Nov 2026)

A third-party research pass on hooks, MCP and sub-agents raised these. Each one needs measuring on the
CLI version we validate against (macOS and Windows) before the lab states it. Don't quote the research.

1. **Hook failure semantics.** The research claims only exit code 2 (or a JSON `deny`) blocks, and other
   non-zero exits (a crash, exit 1) let the tool call through. Our plan assumed command errors deny and only
   timeouts fail open. Test exit 0, 1, 2, a crash, invalid JSON and a timeout, in the CLI and in VS Code,
   and put the measured table in the Step 6 walkthrough.
2. **Routing around a guard.** Test our Step 6 guard against an agent with a shell: an inline interpreter
   (`python -c`, `pwsh -Command`), path tricks (`../`, a symlink), and the file-read or edit tool instead of
   the shell. Record what gets through; that's the "limits" section, with evidence.
3. **Where hooks are read from.** Confirm whether the Copilot CLI reads hook blocks from `.claude/settings.json`
   (the research says yes; the reference we checked lists `.github/hooks/*.json` and
   `.github/copilot/settings(.local).json`). Matters for "agent config files are executable code" and for
   which files `CODEOWNERS` should protect.
4. **Policy hooks** (confirmed in the hooks reference: machine-wide, load first, ignore `disableAllHooks`,
   CLI only). Candidate for one slide in Step 6: repo hooks for project quality, policy hooks for security.
   Check whether VS Code has an equivalent for managed machines.

Ideas from the same research, for later (not scheduled):

- **LSP for the CLI** (`.github/lsp.json` in the repo): a small optional exercise, if a C# language server can
  be installed on a locked-down machine.
- **"Spot the poisoned MCP tool"**: an optional MCP track with a tiny C# stdio server (no Node or Docker),
  one honest tool and one with an injected instruction in its description.
- **A blind reviewer sub-agent** (one that hasn't seen the conversation) as a Try in Step 3.
