# Step 8: the stakeholder, the roles and the process

Step 8 is the only step that needs a second person. This page explains why, who does what, and how
to run the three arms so the comparison is fair. Read it with
[step08.md](step08.md); it doesn't spoil the results.

## Why there's a stakeholder

Steps 1 to 7 each close a gap the code can't show, but somebody on the team already knows the answer:
a rule, a procedure, a limit. Step 8 is different. Backlog item B1 (`docs/backlog.md`) is four vague
bullets, and **nobody has written down the decisions yet**: which priority values, how ties sort, whether
"due before" includes the date, what "overdue" means and in which time zone, what happens with a bad value.

In real work those decisions sit in a product owner's head. In the lab they sit in the answer key,
[.lab/facilitator/step08/answer-key.md](../facilitator/step08/answer-key.md), and the 8 hidden
acceptance tests check them. The agent can't read the key, just as it can't read your product owner's
mind. It gets a decision right only if it **asks someone who knows**. The stakeholder is that someone.

So Step 8 measures one thing: **how good is the agent at finding out what it doesn't know?** That
only works if the stakeholder gives away nothing the agent didn't ask for.

## The roles

| Role | Who | Does | Doesn't |
|---|---|---|---|
| **Driver** | The participant | Opens each new session, pastes the prompt, types the stakeholder's answers into the chat, runs `./lab reset` and `./lab apply 8`, reads the spec files | Answer the agent's questions from their own head, or hint |
| **Stakeholder** | A partner, or the facilitator, or the driver wearing a second hat (see "Running it alone") | Holds the answer key and answers each question the agent asks, strictly from it | Volunteer, summarise, correct the agent, or answer questions that weren't asked |
| **Facilitator** | The workshop leader | Owns the key, settles "does that row answer this question?", runs the hidden tests, leads the comparison | Coach the agent |
| **Agent** | The default Copilot agent (with Spec Kit's skills in run 3) | Builds B1, and asks questions when the prompt or the process tells it to | — |
| **Lab wizard** | The `lab-wizard` agent, in its own chat | Shows the prompts and the checklist, asks before `reset` and `apply`, summarises each run | Play the stakeholder or score Step 8 (the hidden tests do that) |

## Before you start

- [ ] Steps 0 to 7 installed (`./lab list`), and no uncommitted changes (`git status --short`).
- [ ] The driver has read B1 and written down three things it doesn't say. Keep the list: you'll compare it
      with the questions the agent asks.
- [ ] The stakeholder has the answer key open **where the agent can't see it**: a printout, another screen,
      or the browser. Never paste it into the chat or attach it.
- [ ] The stakeholder has somewhere to note each question and which row they gave (a tally is enough).
- [ ] About 25 minutes: roughly 5 for run 1, 5 for run 2 and 12 for run 3, plus reading and comparing.

## The three runs

Each run starts from the same place: Steps 0 to 7 installed, nothing else. Use a **new** chat with the
**default agent** each time (or each phase, in run 3).

### Run 1: one prompt (no stakeholder)

```text
Implement backlog item B1 from docs/backlog.md.
```

- **Driver:** paste it and let it finish.
- **Stakeholder:** nothing. If the agent asks anything, the only answer is "Use your best judgement."
  This is the baseline: what an agent does when nobody tells it the decisions.
- **Then:** score it (see "Scoring"), and `./lab reset`.

### Run 2: ask me first

```text
Implement backlog item B1 from docs/backlog.md. Before you write any code, ask me the questions you need answered, then stop and wait for my answers.
```

- **Driver:** paste it. The agent replies with a list of questions and stops.
- **Stakeholder:** answer each question by the rules below. The driver types the answers into one reply,
  numbered like the agent's questions.
- **Driver:** then send exactly: `That's everything. Now implement B1.` If the agent asks more questions
  partway through, the stakeholder answers those by the same rules.
- **Then:** score it, and `./lab reset`.

### Run 3: Spec Kit

`./lab apply 8` installs Spec Kit and the team's constitution (Windows: `.\lab apply 8 --tools powershell`).
Then one **new chat per phase**; the files carry the state between them:

| Phase | Driver types | Stakeholder | Driver reads before the next phase |
|---|---|---|---|
| 1 | `/speckit-specify Implement backlog item B1 from docs/backlog.md.` | – | `specs/001-*/spec.md`: look for `[NEEDS CLARIFICATION]` markers |
| 2 | `/speckit-clarify` | Answers each question by the rules (clarify asks one at a time, often with options) | The `## Clarifications` section of `spec.md` |
| 3 | `/speckit-plan` | – | `plan.md` |
| 4 | `/speckit-tasks` | – | `tasks.md` |
| 5 | `/speckit-implement` | Answers any question by the rules | – |

Reading the spec after clarify matters: a phrase like "a clear validation message" or "stands out" is
a decision dressed up as a requirement. In the lab, don't add your own clarifications (that would be
the driver acting as stakeholder); just note what you spotted, for the comparison.

## How the stakeholder answers

The rule: **for each question, give the key's rows that answer that question, word for word, and
nothing else.** If no row answers it, say **"No preference."**

| The agent... | Say | Don't |
|---|---|---|
| Asks something a row answers ("What priority values?") | That row, verbatim | Add related rows ("...and unknown values get a 422") |
| Asks something two rows answer | Both rows, verbatim | Merge them into your own summary |
| Asks a two-part question | The row for each part, labelled | Answer only the easy part, or more than both parts |
| Asks something the key doesn't cover ("Should I add tags?", "Which colour?") | "No preference." | Invent a rule, or say "Up to you, but I'd..." |
| Proposes a default ("I'll assume due dates have no time. OK?") | The matching row, verbatim, whether it agrees or not | Just "Yes" (that confirms the agent's wording, not the key's) |
| Offers options (Spec Kit clarify: A, B, C) | The option that matches the row, plus the row verbatim. No match: the row. Not covered: "No preference." | Pick the agent's recommended option because it's quicker |
| Asks a question in run 1 | "Use your best judgement." | Anything from the key |
| Asks something you can't place | Ask the facilitator which row, if any, answers it | Guess |
| Gets something wrong later | Nothing. Only answer questions | Correct it: that's coaching |

Why so strict? Every extra word is a decision the agent didn't have to find. When we validated this step,
the first stand-in stakeholder volunteered two decisions nobody asked about, and the "ask first" scores
went up for the wrong reason. Those runs were thrown away and redone.

## Running it

- **In pairs (best):** one driver, one stakeholder with the key. To both see all three runs, keep the same
  roles for the whole step; swapping halfway changes how strictly the questions are answered.
- **Facilitator as stakeholder for a room:** groups do run 1 together; for runs 2 and 3, each group
  reads out its agent's questions and the facilitator answers from the key. Slower, but very consistent.
- **Running it alone:** you're both driver and stakeholder. Open the key in another window and follow the
  rules strictly; it helps to read each question aloud and point at the row before typing. You'll know the
  decisions after run 1's scoring, so be disciplined: answer what's asked, nothing more.

## Scoring

The hidden acceptance tests are the stakeholder's, not the agent's, so the facilitator (or the driver,
after the run) scores each arm:

```bash
cp .lab/scripts/checks/LabCheck08.cs tests/Tasks.App.Tests/
dotnet test --filter LabCheck08          # count the passes, out of 8
rm tests/Tasks.App.Tests/LabCheck08.cs   # Windows: del tests\Tasks.App.Tests\LabCheck08.cs
```

Delete the file before `./lab reset` or the next run, so the agent never sees it. The checklist in
[step08.md](step08.md) says which decision each test checks.

## Comparing the three runs

Put the three side by side, then read the rest of [step08.md](step08.md):

- **Score** out of 8 for each run.
- **Questions:** how many each arm asked, and which decisions **nobody** asked about. Match each failed
  test to a question that was never asked.
- **Your list:** which of the driver's three "things B1 doesn't say" did the agent ask about?
- **Cost:** wall time and the number of sessions for each arm.

## Common mistakes

- **The stakeholder volunteers or summarises.** You end up measuring the stakeholder, not the agent.
- **Answering "yes" to the agent's proposal.** It locks in the agent's wording; give the row instead.
- **Reusing a session** for the next run or the next Spec Kit phase. Always a new chat.
- **The key in the agent's reach:** pasted, attached, or left open as an editor tab the agent can see.
- **Skipping the reads in run 3.** The spec is the point where vague requirements become visible.
- **Forgetting `./lab reset`** between runs 1, 2 and 3, or leaving `LabCheck08.cs` in the test project.
