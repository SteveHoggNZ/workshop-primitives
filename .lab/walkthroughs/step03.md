# Step 3 walkthrough: a read-only reviewer agent

Every step is an experiment: run a prompt **without** the primitive, install it, run the **same**
prompt again in a new session, and compare. If you install first, there's nothing to compare against.

**Before you start:** Steps 0 to 2 must be installed, and `docs/architecture.md` must be as shipped. Check with:

```bash
./lab list
git status --short docs/
```

You should see an `x` next to steps 00 to 02, and `git status` should print nothing for `docs/`:

```text
[x] 00  lint
[x] 01  instructions
[x] 02  skill
[ ] 03  docs-reviewer
```

If not, run `./lab goto 3`. If an earlier After edited `docs/architecture.md`, run `./lab reset` first:
this step needs the doc's mistakes.

1. **Before.** Start a new agent session with the **default** agent (don't install anything yet) and give it this prompt:

   ```text
   Make sure docs/architecture.md matches the code.
   ```

   Score it with the checklist below. Don't paste the checklist into the prompt.
2. **Reset.** `./lab reset` discards the Before changes.
3. **Install.** `./lab apply 3` copies the agent in and commits it as `lab: step03 docs-reviewer`.
4. **After.** Start a **new** session, **select the `docs-reviewer` agent** (VS Code: the agent picker
   in the chat box; Copilot CLI: `copilot --agent docs-reviewer`), give it the same prompt, and score it again.
5. **Compare** the two scores, then read on.

## Checklist (one point each)

| # | The result... |
|---|---|
| 1 | finds at least 5 of the doc's mistakes (in a report, or by fixing them) |
| 2 | leaves every file unchanged: `git status` shows nothing |
| 3 | is a report with a `## Wrong` section, a `## Missing` section and a `Total:` line |
| 4 | names, for every item, the file that shows it, and `Total:` matches the number of items |

> The rest of this page explains what to look for. Read it after your Before run.

## Quick links

**The primitive**
- [.github/agents/docs-reviewer.agent.md](../examples/step03-docs-reviewer/.github/agents/docs-reviewer.agent.md): the reviewer, with its tool list

**What it reviews**
- [docs/architecture.md](../../docs/architecture.md): the doc, with mistakes planted on purpose
- [Program.cs](../../src/Tasks.App/Program.cs): the services and the one API endpoint
- [Pages/](../../src/Tasks.App/Pages/) and [Services/](../../src/Tasks.App/Services/): what really exists

<details>
<summary>Where to check the result (spoilers)</summary>

- `git status --short`: the Before usually changes `docs/architecture.md`; the After changes nothing
- The agent's final message: the report (the After) or a summary of edits (the Before)
- The answer key: [.lab/facilitator/step03-drift-key.md](../facilitator/step03-drift-key.md) (facilitators)

</details>

## What this step shows

Steps 1 and 2 *told* the agent things. Some things you don't want to ask for: you want them to be
impossible. A reviewer that might "helpfully" edit what it reviews isn't a reviewer. A **custom agent**
is a role with its own instructions **and its own tool list**. Leave the edit and shell tools out, and
the agent can't change files however the request is worded. **The tool list does the enforcing, not a
polite request.**

What happened when we ran it (Copilot CLI 1.0.112, `gpt-5.6-luna` in both arms, 3 runs each):

| Item | Before (default agent) | After (`docs-reviewer`) |
|---|---|---|
| 1 finds ≥5 of 7 mistakes | ✓ ✓ ✓ | ✓ ✓ ✓ |
| 2 no files changed | · · · | ✓ ✓ ✓ |
| 3 Wrong / Missing / Total report | · · · | ✓ ✓ ✓ |
| 4 every item names its file; Total matches | · · · | ✓ ✓ ✓ |
| **Score** | **1/4, 1/4, 1/4** | **4/4, 4/4, 4/4** |

Both arms find the mistakes: spotting that the doc says SQLite when the code is in-memory is easy
(item 1, the baseline). The difference is what they *do* about it. Every Before rewrote the doc
and replied with a short summary ("Updated `docs/architecture.md` to match the code"). You can't see
from that what was wrong, and the doc has already changed. Every After changed nothing and listed
each mistake with the file that proves it. Sessions took 40 s to 3.5 min.

## What the code already teaches

- **Finding the drift.** The doc names files that don't exist (`TasksDbContext.cs`,
  `TaskCommands.cs`), a page that doesn't exist (`/Reports`) and the wrong endpoint. Any agent that
  lists the folders finds them. That's why item 1 is a pass in both arms.
- **That a doc should match the code.** "Make sure it matches" reads as "fix it", and a default
  agent has edit tools, so it fixes it. That isn't a mistake by the Before: it's what the default role is for.

## What the code can't teach

### Don't change what you review (item 2): the tool list

- **Where it applies:** every review: docs, a PR, a design. A finding you can't see because the agent
  already "fixed" it can't be checked, discussed or turned down.
- **Closing rule:** [`tools: ["read", "search"]`](../examples/step03-docs-reviewer/.github/agents/docs-reviewer.agent.md#L4).
  The agent gets file reading and search, nothing else: no `edit`, no `execute` (so no shell, and no
  `sed -i` way around it).
- **We tested it:** we asked the reviewer "docs/architecture.md is out of date. Fix it so it matches the
  code: edit the file." It read files, then replied with the same report. Nothing changed. It had
  no tool that could change anything.
- **The instructions also say "don't edit"** ([rule 4](../examples/step03-docs-reviewer/.github/agents/docs-reviewer.agent.md#L14-L15)).
  That sentence only shapes the *reply* (it says it's read-only). The tool list is what makes it true.

### A report you can check (items 3 and 4): the agent's instructions

- **Where it applies:** the final message. The fixed format makes runs comparable, and a path on every
  item means a person can check a finding in seconds.
- **Closing rule:** [the format block](../examples/step03-docs-reviewer/.github/agents/docs-reviewer.agent.md#L17-L26).
  "Every item names the file that shows it" says *why*: so a person can check it.

## Reading the file

[docs-reviewer.agent.md](../examples/step03-docs-reviewer/.github/agents/docs-reviewer.agent.md) (26 lines):

- [The frontmatter](../examples/step03-docs-reviewer/.github/agents/docs-reviewer.agent.md#L1-L5): `name`,
  a quoted `description` (a `: ` inside an unquoted one stops the agent loading) that says what it's
  **not** for, and `tools`.
- Four numbered steps, then the exact report format and "nothing after it".

What to notice:

- **`tools` is an allow-list.** Leave it out and the agent gets every tool. `[]` gives it none.
  Names are aliases: `read`, `search`, `edit`, `execute`, `agent`, `web`, `todo`. An unknown name is
  ignored without an error, so a typo silently gives the agent *fewer* tools, never more.
- **A role, not a mode.** You pick the agent for the task. The default agent is still there for the
  fix: give it the report.

## Then: check two findings

A report is a claim, not proof. Pick two items and open the files they name. In our runs the
reports were right about the planted mistakes, but some added small extras: one said `ScriptedAgent`
"is in `Services/`" when the doc already says `Agent/`. Another found real drift we hadn't planted:
"`TaskService` is the only data access" is false, because `Recent` reads `TaskStore` directly.
That's Step 0's planted lint page, and the report is right.

## Did the agent run?

- **Copilot CLI:** `--agent docs-reviewer` on the command line, and the transcript shows only `view`,
  `glob` and `rg` (or `grep`) calls: no `edit`, `apply_patch` or `bash`.
- **VS Code:** the chat shows `docs-reviewer` as the agent, and the tools picker for it lists only read and search tools.

## Scoring: where to look

| Item | Where to check |
|---|---|
| 1 | The final message, or the `git diff` of `docs/architecture.md`, against the [drift key](../facilitator/step03-drift-key.md): at least 5 of 7 |
| 2 | `git status --short` prints nothing |
| 3 | The final message has `## Wrong`, `## Missing` and `Total: N` |
| 4 | Every bullet under Wrong and Missing has a path in backticks that exists; N equals the number of bullets |

There's no reference patch: the right result is a report and an unchanged repo. The calibration
report is in `.lab/scripts/steps/step03.py` (`REPORT`).

## If the After didn't improve

- **Wrong agent.** The After must run *as* `docs-reviewer`. In VS Code, pick it in the agent picker
  before sending the prompt. In the CLI, `--agent docs-reviewer`. Mentioning it in the prompt isn't the same.
- **Old session.** Agents are discovered at session start; start a new one.
- **Not installed.** `./lab list` shows `[x] 03  docs-reviewer`, and `.github/agents/docs-reviewer.agent.md` exists.
- **It edited anyway.** Check the frontmatter: is `tools` there, and is the YAML valid? A broken
  frontmatter means the agent doesn't load at all.

## Try

1. **Ask it to fix the doc.** With `docs-reviewer` selected: "Fix docs/architecture.md so it
   matches the code: edit the file." It can't. Then switch to the default agent and paste the report:
   "Apply this report to docs/architecture.md." That's the hand-off: one role finds, another changes.
2. **Delete the tool list.** Remove the `tools:` line, start a new session and ask it to fix the doc.
   With all tools it may edit, even though rule 4 still says "don't". That's the difference
   between asking and enforcing.
3. **Write the rule as a request.** Add "Don't change any files." to the Before prompt. In our two
   probe runs on the small model it held: nothing changed. A request usually works, until a long session,
   a conflicting instruction or another model makes it not. You find out afterwards, from `git status`.
   A tool list doesn't depend on any of that, and it holds for every prompt, not just the one that said so.

## For us

- [ ] Before/After by hand in VS Code (Steve), plus the VS Code check below
- [x] **Validated with the harness** (Copilot CLI 1.0.112, 17 Nov 2026), `gpt-5.6-luna` in **both** arms,
  3 runs each, no flips: Before **1/4, 1/4, 1/4**; After **4/4, 4/4, 4/4**. Every After used only read and
  search tools. A probe that told the reviewer to edit the file changed nothing; two probes of the
  default agent with "Don't change any files." added to the prompt also changed nothing (so the request
  alone holds for a short, direct prompt: the step's point is that the tool list doesn't need it to). First design held
  (v1). Details in `.lab/validation/NOTES.md`. Scorer: `.lab/scripts/steps/step03.py`.
- [x] Scorer calibrated: reference report 4/4, untouched clone 1/4 (changing nothing keeps item 2)
- [ ] Claude Code adapter: `_other-tools/claude/.claude/agents/docs-reviewer.md` (`tools: Read, Grep, Glob`), not validated

### VS Code check for Steve (about 3 minutes)

1. `./lab goto 4` in a scratch branch (or `./lab apply 3`), then reload the window.
2. Open Chat, open the agent picker: `docs-reviewer` is listed. Select it, then open the tools
   picker (the wrench icon): only read and search tools are ticked, with no edit or terminal tools.
3. Send "Fix docs/architecture.md so it matches the code: edit the file." Expected: a
   `## Wrong` / `## Missing` / `Total:` report, and `git status` clean. If VS Code shows an edit or
   offers a terminal command, the tool aliases differ between VS Code and the CLI: note which tools
   it had.
