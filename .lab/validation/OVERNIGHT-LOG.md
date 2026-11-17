# Overnight log

Append-only. One entry per event, local time first, ending with "Next:". Run ids are in `../.lab-runs/`;
scores in `results.jsonl`, cost and time in `metrics.jsonl`. Times below are from commits, the reflog and
`results.jsonl`; untimed work has no entry time of its own.

## Step 2 (written up after the fact, 17 Nov 2026)

- **19:21** Step 2 started from `../.step02/base` in a worktree on branch `step02`. Base app 11 tests pass;
  lint `Total: 6`; untouched hidden checks 3 failed. Found `try.cs` showing stale output after a source
  change (the file-based app cache misses changes in the referenced project); skill now runs it with
  `--no-cache` (+~1 s). Next: commit.
- **19:29** Commits `7d93ade` (in-app agent at `/Agent`) and `f8a6bfb` (skill, scorer, checks, solution).
  `goto 2/3 [--solutions]` checked in a scratch clone. Next: calibrate.
- **19:42** Step 1 re-calibrated after the base change: `s01-after-calib-ref2` 6/6, `s01-before-calib-base2` 0/6.
  Next: calibrate Step 2.
- **19:49** Step 2 v1 calibration: `s02-after-calib-ref` 6/6, `s02-before-calib-base` 0/6. Next: agent runs.
- **19:58** v1 runs scored: Before (gpt-6.1-sol) `r1` 5/6, `r2` 5/6; After (gpt-5.6-luna) `r1` 5/6, `r2` 6/6.
  Step 1 regression: `s01-before-gpt-6.1-sol-r3` 1/6, `s01-after-gpt-5.6-luna-r3` 6/6 (holds).
  Not validated: only item 6 separated the arms. Transcripts: both Befores reached `docs/agent-commands.md`
  through the link in architecture.md's `/Agent` row; a strong model writes "Deleted 2 completed tasks."
  unprompted. After `r1` ran `try.cs` but pasted it without the `> ` lines. Next: redesign.
- **20:08** Design v2, commits `02decd5` (base: optional `Intent.Preview` for the confirm prompt, unused;
  docs link dropped; `step01.patch` context updated) and `30fbae1` (item 3 is now "the confirm prompt says
  how many"; scorer fix for item 6: match the conversation inside a fenced block). Negative control: the
  Befores' code fails the new check. Next: calibrate v2.
- **20:22** v2 calibration: `s02-after-calib-ref2` 6/6, `s02-before-calib-base2` 0/6; `s01-after-calib-ref3` 6/6.
  Next: v2 runs.
- **20:33** v2 runs: Before `v2r1` 3/6, `v2r2` 3/6 (both used `Preview` but without a count); After `v2r1` 5/6,
  `v2r2` 5/6. Both Afters loaded the skill and ran `try.cs`, then summarised it ("try script confirmed").
  Next: tighten the skill's paste instruction.
- **20:34** Design v3 (skill only), commit `0c820f9`: "paste the output unchanged in a code block; a summary
  doesn't count", with the reason. The v2 Befores stand: the Before arm has no skill. Next: two Afters.
- **20:41** v3 runs: After `v3r1` 6/6, `v3r2` 6/6, both pasted the conversation. **Step 2 validated:
  Before 3/6, 3/6; After 6/6, 6/6.** Next: write up.
- **20:42** Commit `19f980d`: walkthrough `step02.md`, NOTES.md (counted, not counted, scorer fix), README
  step list, wizard step note. `main` fast-forwarded to it. Next: clean up.

Lessons for later steps:

- **Check what the Before can reach.** A link in a doc the agent reads anyway gave away a checklist item.
  Before calibrating, grep the base for every file a checklist item names.
- **Don't score what a strong model does unprompted.** Read one Before transcript before trusting an item.
  A planted, unused hook (Step 1's `_Empty`, Step 2's `Preview`) separates "the code shows how" from
  "the team says when".
- **Small models trim final messages.** If an item needs something in the final message, the primitive
  must say so in bold, with the reason, not "paste the output".
- **Test a script on the edit loop, not once.** Change the code and run it again; caches lie.
- **Score warm.** Clones the agent built score in seconds; cold calibration clones take about 2.5 minutes.
- **A skill-only change keeps the Before runs.** Re-run only the arm that changed, and say so in NOTES.md.
- **zsh doesn't word-split `$var`.** Put multi-run loops in a `bash` script.
- **`COPILOT_HOME` must be absolute** when the command runs inside a clone.
- **`git branch -d` checks the current branch** (`lab/steve` in the main worktree), not `main`.

## Clean-up, 17 Nov 2026

- `step02` branch deleted (same commit as `main`), worktrees pruned, run clones cleaned (transcripts and
  `.json` kept), `metrics.jsonl` and this log added.

Next: Step 3 (read OVERNIGHT.md)

## Overnight session, 17–18 Nov 2026 (Steps 3–8)

- **21:23** Resumed from "Next: Step 3". Copilot CLI 1.0.112; `gpt-5.6-luna` accepted (smoke run OK).
  `lab/steve` no longer exists: Steve reset it, switched the main checkout to `main` and deleted the branch
  before the session, so there is no Steve branch to leave alone. Working in the main checkout on `main`.
  Push to origin allowed tonight (Steve, in chat), never force. Next: health-check `main` in a scratch clone.
- **21:26** Health check of `main` (552e939) in scratch clones: `dotnet test` 11 passed; `goto 1/2/3` and
  `--solutions` all OK (tests 11/13/15 passed); lint with step 0 `Total: 6 (baseline 6)`. Nothing to fix.
  Whole check 728 s, mostly cold restores and builds in 7 clones. Next: harness improvements (run command with
  metrics, one build per score).
- **21:40** Harness commits `e28015e` (one build per score, verified identical on 4 calibration clones scored
  with old and new code; `run`, `calibrate`, `metrics`, parallel prepare/score) and 6b86da3 (compact usage, probe prompts).
  Next: Step 3.

### Step 3: read-only reviewer

- **21:35** Step 3 started. Design v1 (from Python step 4): prompt "Make sure docs/architecture.md matches the code."
  in both arms; After selects `docs-reviewer` (`tools: ["read", "search"]`). Checklist: 1 baseline finds ≥5/7 drift
  (report or doc fix); 2 lesson no files changed; 3 lesson fixed format (## Wrong / ## Missing / Total); 4 lesson
  every item cites an existing path and Total matches. Desk check: nothing in the base names the format or the
  agent; AGENTS.md doesn't say to edit the doc on a review. Commit `cd8c7eb`. Calibration: ref 4/4, untouched 1/4
  (changing nothing keeps item 2, as expected). Next: pilot.
- **21:40** Pilot: `s03-before-gpt-5.6-luna-v1-r1` 1/4 (found 7/7 and fixed the doc, summary only, 51 s);
  `s03-after-gpt-5.6-luna-v1-r1` 4/4 (only view/glob/rg, 7 items with paths, 39 s). Before lost on the gap, not
  competence. Next: r2, r3 per arm + one probe asking the reviewer to fix the doc.
- **21:50** Full round v1: Before r2, r3 1/4; After r2, r3 4/4 (all used only read/search tools). Probes: reviewer
  told to edit, changed nothing; default agent + "Don't change any files." changed nothing (2/2). Gate passes.
  Commit `ce17224` (walkthrough, notes, README, wizard). Next: independent review.
- **22:05** Review findings (cold subagent): must-fix, the score didn't show the tool list did the enforcing (no
  After tried to edit). Control probes: `tools:` line deleted + "edit the file" → 2/3 edited; with tools 0/2
  (one pushed to `sed -i`). Should-fix scorer: Total clash with AGENTS.md's lint Total, numbered lists and link
  paths, loose path check, `Reports` regex. Fixed; re-scored all 8: unchanged. Windows note, wording nits.
  Next: commit, then Step 4.
