# Step 8 walkthrough: spec-driven development

This step has **three** runs, not two: the same vague backlog item done three ways. The second one
is there so you can tell what the process buys from what simply asking buys.

**Before you start:** Steps 0 to 7 must be installed. Check with:

```bash
./lab list              # Windows: .\lab list
```

You should see an `x` next to steps 00 to 07. If not, run `./lab goto 8`. Read
[docs/backlog.md](../../docs/backlog.md), item B1. It's four bullets. Write down three things it
doesn't say.

You'll need a partner (or the facilitator) to play the **stakeholder**, with the answer key
([.lab/facilitator/step08/answer-key.md](../facilitator/step08/answer-key.md)). The stakeholder
answers **only what's asked**, in the key's words.

1. **One prompt.** New session:

   ```text
   Implement backlog item B1 from docs/backlog.md.
   ```

   Score it (below), then `./lab reset`.
2. **Ask me first.** New session:

   ```text
   Implement backlog item B1 from docs/backlog.md. Before you write any code, ask me the questions you need answered, then stop and wait for my answers.
   ```

   The stakeholder answers each question from the key, and only those. Then: "That's everything.
   Now implement B1." Score it, then `./lab reset`.
3. **Install.** `./lab apply 8` installs Spec Kit (its skills and `.specify/`) and the team's
   constitution, and commits them as `lab: step08 spec-kit`. On Windows: `.\lab apply 8 --tools powershell`
   (the same skills, calling the PowerShell scripts).
4. **Spec Kit.** One **new session per phase**, and read each file before the next phase:
   1. `/speckit-specify Implement backlog item B1 from docs/backlog.md.` → read `specs/001-*/spec.md`
   2. `/speckit-clarify` → the stakeholder answers each question it asks, from the key
   3. `/speckit-plan` → read `plan.md`
   4. `/speckit-tasks` → read `tasks.md`
   5. `/speckit-implement`

   Score it.
5. **Compare** the three, then read on.

## Checklist (one point each)

The facilitator runs 8 hidden acceptance tests (`.lab/scripts/checks/LabCheck08.cs`). Each is one
decision B1 leaves open:

| # | The result... |
|---|---|
| 1 | a new task defaults to priority `normal`, no due date |
| 2 | a task can be created with a priority and a due date (`dueDate`, `YYYY-MM-DD`) |
| 3 | `GET /api/tasks?sort=priority` sorts urgent, high, normal, low, with the agreed tie-breaks |
| 4 | `?priority=high` filters, in creation order |
| 5 | `?due_before=` is inclusive and leaves out undated tasks |
| 6 | "overdue" uses today in Auckland, and done tasks are never overdue |
| 7 | PATCH can clear the due date; an unknown priority gets a 422 |
| 8 | the task list page shows each task's priority and the word `Overdue` |

> The rest of this page explains what to look for. Read it after all three runs.

## Quick links

- [.specify/memory/constitution.md](../examples/step08-spec-kit/.specify/memory/constitution.md): the team's non-negotiables, checked by every phase
- [.github/skills/speckit-*/SKILL.md](../examples/step08-spec-kit/.github/skills/): Spec Kit's phases, as skills
- [docs/backlog.md](../../docs/backlog.md): B1
- [.lab/facilitator/step08/answer-key.md](../facilitator/step08/answer-key.md): the stakeholder's answers (facilitator)

## What this step shows

Every earlier step closed a gap the code couldn't show: a team rule, a procedure, a limit. B1's
gap is different: **nobody has decided yet.** "Sorted by priority": which priorities, in what order,
ties how? "Overdue": before today where? Whoever writes the code makes those decisions, and an
agent makes them silently.

What happened when we ran it (Copilot CLI 1.0.112, `gpt-5.6-luna` in every arm, 3 runs each,
Spec Kit 1.2.5, constitution 1.1.0):

| Arm | Hidden tests (of 8) | Sessions | Agent time | AI credits |
|---|---|---|---|---|
| One prompt | **1, 0, 1** | 1 | about 3 min | about 3.4 |
| Ask me first | **8, 7, 8** | 2 (questions, then answers) | 3–6 min | 4–6.5 |
| Spec Kit + constitution | **6, 8, 7** | 9 (specify, clarify × 4, plan, tasks, implement) | 10–12 min | 15–21 |

- **One prompt guesses.** Every one-shot run built a working feature with tests that pass, and
  failed almost every acceptance test: `low/medium/high`, its own sort, its own validation.
- **Asking gets most of the gain, cheaply.** Six questions each; the runs that asked about the page
  got 8/8. The one that didn't ask how the list page should show tasks got 7.
- **Spec Kit does as well, at five times the cost.** For one small item, that's poor value. Its
  value is in the artefacts: `spec.md`, `plan.md`, `tasks.md` on disk, so each phase starts fresh
  and a reviewer can read the decisions.
- **Its misses are the spec's silences.** Both Spec Kit misses on item 7 had a spec that said only
  that the API rejects bad input with "a clear validation response" (r1) or "a clear client error" (r3).
  That's a contract decision the spec didn't mark, so `/speckit-clarify` never asked, and the code
  returned 400, not 422. The run whose first clarify question happened to include validation errors
  (r2) got the 422, and 8/8. Reading `spec.md`
  yourself before `/speckit-plan` is where you'd catch it: that review is the point of the phase.

## What the constitution did

[Article VI](../examples/step08-spec-kit/.specify/memory/constitution.md#L28-L33) says: never invent
a public contract; mark field names, accepted values, sort and filter semantics, validation
responses and derived states like "overdue" as `[NEEDS CLARIFICATION]`. Every Spec Kit spec came
out with exactly three markers (API contract, sort and filter semantics, overdue), and clarify asked
about each. The Python version of this lab found that without that article, the spec invented the
API and clarify never asked. It also shows Article VI's limit: "validation responses" is in the
list, and two specs still wrote "a clear validation response" without a marker. A rule in a document
is read by a model, not enforced.

Other articles paid off quietly: "today ... uses the user's time zone" (II) and the earlier steps'
`AGENTS.md` and gate all apply in every phase.

## Choosing the ceremony

| Situation | Use |
|---|---|
| A small change with a few open decisions | Ask first. One sentence in the prompt |
| Larger work, several sessions, more than one person | Spec Kit (or any spec → plan → tasks flow): the decisions live in files, each phase starts fresh |
| A change with no open decisions | Neither: Steps 1–7 cover it |

"Isn't this waterfall?" No: one small feature, a spec you can read in two minutes, revised in the
loop, and turned into tests.

## Reading the files

- [constitution.md](../examples/step08-spec-kit/.specify/memory/constitution.md) (35 lines): six
  articles, each one line of rule and one of reason. Short on purpose: every phase reads it.
- `specs/001-*/spec.md` after specify: user stories, functional requirements, and the
  `[NEEDS CLARIFICATION]` markers. After clarify: a `## Clarifications` section with the answers.
- `plan.md`, `research.md`, `data-model.md`, `contracts/`: how, in this codebase.
- `tasks.md`: 25–30 small tasks, tests first (the constitution's Article III).

## Running it on Windows

`./lab apply 8 --tools powershell` installs the same skills pointing at `.specify/scripts/powershell/`.
The PowerShell variant is generated by the same Spec Kit version but we haven't run it.

## Scoring: where to look

Copy `.lab/scripts/checks/LabCheck08.cs` into `tests/Tasks.App.Tests/`, run
`dotnet test --filter LabCheck08`, count the passes, delete the file. The facilitator usually does
this; the tests are the stakeholder's, not the agent's.

The reference solution is [.lab/solutions/step08.patch](../solutions/step08.patch) (`./lab goto 9 --solutions`).

## If the Spec Kit run did badly

- **Clarify didn't ask about something.** Check the spec: was it marked `[NEEDS CLARIFICATION]`? If
  not, the spec made the decision. Edit the spec or re-run specify; don't skip ahead to plan.
- **The stakeholder volunteered answers.** Only what's asked; otherwise you're measuring the stakeholder.
- **Sessions reused.** Each phase in a new session: the files carry the state.

## For us

- [ ] Before/After by hand in VS Code (Steve), plus the VS Code check below
- [x] **Run with the harness** (Copilot CLI 1.0.112, 17 Nov 2026), `gpt-5.6-luna`, 3 runs per arm. One prompt
  **1, 0, 1**; ask first **8, 7, 8**; Spec Kit **6, 8, 7**. The stakeholder (Claude, from the answer key,
  answering only what was asked) is in the transcripts. Details in `.lab/validation/NOTES.md`.
  Scorer `.lab/scripts/steps/step08.py`, hidden tests `.lab/scripts/checks/LabCheck08.cs`.
- [x] Scorer calibrated: reference solution 8/8, untouched clone 0/8
- [ ] Spec Kit implement took 3.5–6.3 min (over the 4-minute session budget in 2 of 3 runs)
- [ ] PowerShell variant (`--tools powershell`) not run

### VS Code check for Steve (about 3 minutes)

1. `./lab goto 9` in a scratch branch (or `./lab apply 8`), then reload the window.
2. In Chat, type `/speckit-`: the phases appear as slash commands. Run `/speckit-specify Implement
   backlog item B1 from docs/backlog.md.` and check `specs/001-*/spec.md` has `[NEEDS CLARIFICATION]`
   markers for the API contract and "overdue".
3. Start `/speckit-clarify` in a new chat and check it asks one question at a time, with options.
