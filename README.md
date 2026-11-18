# Coding-agent primitives lab (.NET)

A hands-on lab for GitHub Copilot's customisation primitives: instructions, skills, custom agents,
hooks, quality gates and spec-driven development. You work on a small task-tracking web app
(ASP.NET Core Razor Pages) and, for each primitive, you **measure** what it changes:

1. Run a prompt in a new agent session **without** the primitive (the Before), and score it.
2. Install the primitive.
3. Run the **same** prompt in another new session (the After), score it, and compare.

Each step targets a gap that a better model doesn't close on its own: something the agent can't know
(information), can't do (capability), shouldn't be trusted to do (enforcement), or hasn't been told
(ambiguity). Every step has been validated with Copilot agents: the After beat the Before in every
run (results below).

New to Copilot? Do a beginner workshop first: see [OTHER_RESOURCES.md](OTHER_RESOURCES.md) for that, related
labs and inspiration.

## What you need

- .NET SDK **10.0.100 or later** (`dotnet --list-sdks`). Older SDKs are refused by `global.json`.
- Git.
- VS Code with the C# extension, and GitHub Copilot (agent mode). Step 0 needs VS Code.
  The other steps also work in the Copilot CLI.
- Step 8 needs a partner (or the facilitator) to play the stakeholder.

No database, Docker, Node or Python is needed for the lab itself.

## Set up (once)

```bash
git clone git@github.com:SteveHoggNZ/workshop-primitives.git
cd workshop-primitives
./lab doctor        # checks the SDK, git and the repo (Windows: .\lab doctor)
./lab warm          # first build, and the packages the skill scripts use (slow once, then fast)
dotnet test
git switch -c lab/<your-name>      # do the lab on your own branch, so main stays clean
code .
```

Then, in VS Code, wait for the C# extension to finish loading the solution. If the Problems panel
stays empty later, run **Developer: Reload Window**. Turn off personal Copilot instructions, skills
and MCP servers for the lab, so the comparison is fair.

## Doing the lab with the lab wizard (recommended)

The lab wizard is a custom agent ([.github/agents/lab-wizard.agent.md](.github/agents/lab-wizard.agent.md))
that walks you through each step. It shows you the prompt and the checklist, scores your Before and
After from the diff and your agent's session, resets and installs steps (after asking you), and then
goes through the explanation. It never does the runs itself: that would make it the primitive.

**Start it:**

- **VS Code:** open Copilot Chat, choose **lab-wizard** in the agent picker, and say
  "Start the lab" (or "Where am I?" to pick up where you left off).
- **Copilot CLI:** `copilot --agent lab-wizard`, then the same.

**How it works:** keep the wizard in one chat, and do every Before and After run in a **separate, new**
chat with the default agent (or the agent the step names). For each step the wizard goes:

1. **Before:** shows the prompt and checklist. You run it in a new chat and come back.
2. **Score:** reads the changes and the session, and gives you a pass/fail table.
3. **Reset and install:** `./lab reset`, then `./lab apply N`, once you say yes.
4. **After:** same prompt, new chat. Come back.
5. **Compare:** Before vs After side by side.
6. **Walkthrough:** what the primitive did and why, with Try exercises.

It doesn't show you anything that would spoil the Before (the explanation or the installed files)
until you've scored it.

## Doing the lab by hand

Each step has a walkthrough in [.lab/walkthroughs/](.lab/walkthroughs/) with the prompt, the checklist
and the loop. Read it **only down to the spoiler line** before your Before run.

```bash
./lab list          # the steps, and which are installed
./lab reset         # throw away a Before (or After) run's changes
./lab apply 3       # install step 3's files, committed as "lab: step03 ..."
./lab goto 5        # catch up: install every step before 5
```

## The steps

| Step | Type | Primitive | Gap | Walkthrough | Validated (Before → After) |
|---|---|---|---|---|---|
| 0 | Harness (lint rules) | Warm-up: a linter the agent can read | – | [step00](.lab/walkthroughs/step00.md) | by hand, in VS Code |
| 1 | Instructions | Project instructions (`AGENTS.md`) and a scoped rule | information | [step01](.lab/walkthroughs/step01.md) | 1/6 → 6/6 |
| 2 | Skill | A skill with a tested script, and a prompt file | information | [step02](.lab/walkthroughs/step02.md) | 3/6 → 6/6 |
| 3 | Custom agent | A read-only reviewer agent: the tool list does the enforcing | enforcement | [step03](.lab/walkthroughs/step03.md) | 1/4 → 4/4 |
| 4 | Skill + harness (`lab tdd`) | A TDD skill with phase state: evidence, not claims | enforcement | [step04](.lab/walkthroughs/step04.md) | 2/5 → 5/5 |
| 5 | Skill | A diagnostics skill: measure first, on a cheaper model | capability | [step05](.lab/walkthroughs/step05.md) | 2/4 → 4/4 |
| 6 | Hook + permissions | Permissions, then hooks | enforcement | [step06](.lab/walkthroughs/step06.md) | 1/4 → 4/4 |
| 7 | Hook | A quality gate at the end of the session | enforcement | [step07](.lab/walkthroughs/step07.md) | 3/5 → 5/5 |
| 8 | Skills + workflow (Spec Kit) | Clarify before you build: one prompt, ask first, Spec Kit | ambiguity | [step08](.lab/walkthroughs/step08.md) | 0–1/8 → 7/8 (both clarifying arms) |

The sessions build up: tell it what it can't know (1–3), evidence rather than claims (4–5), rules
rather than requests (6–7), and clarify before you build (8).

**Timing:** every lab command finishes in seconds. Agent sessions take about 1 to 4 minutes each;
Step 8's Spec Kit arm is several sessions and takes about 10 minutes.

## Tips

- **Always start a new chat** for each Before and After run. An old session remembers the last run.
- Run tests with `dotnet test` in the terminal. The editor's test runner is much slower.
- Stuck or out of order? `./lab list` shows where you are; `./lab goto N` gets you to step N.
- Windows: use `.\lab` in PowerShell (or `lab` in cmd) wherever this says `./lab`.
- `./lab help` lists every command.

## The app

Settings come from environment variables: `APP_TZ` (default `Pacific/Auckland`), `APP_NOW` (pin the
clock, ISO 8601), `APP_SEED=false` (start with no tasks), `APP_PORT`.

```bash
dotnet run --project src/Tasks.App    # then open the URL it prints
```

Some problems in the app are planted on purpose (for example in `/Recent` and `docs/architecture.md`).
The steps rely on them, so don't fix them outside a step.

## For facilitators

Everything behind the lab is in [.lab/](.lab/): the step files (`examples/`), reference solutions,
answer keys (`facilitator/`), the validation harness (`scripts/lab.py`) and the results and notes
(`validation/`). Start with [.lab/validation/NOTES.md](.lab/validation/NOTES.md) and
[.lab/validation/OVERNIGHT-REPORT.md](.lab/validation/OVERNIGHT-REPORT.md).
