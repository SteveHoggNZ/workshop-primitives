---
name: tdd
description: "Fix a bug or add behaviour test-first (red, green), one behaviour at a time, with ./lab tdd recording the phase. Use when behaviour is wrong: wrong output, an error page, an exception, a wrong date. Not for slowness, CPU or memory problems (measure those first), visual-only glitches, docs, config or infrastructure."
---

# Test-first change

The team's rule: **a fix isn't done until a test has failed without it and passes with it.** A test
written after the fix may never have caught the bug. `./lab tdd` records the phase in `.tdd-phase`
and checks each step, so the red is evidence, not a claim. (Windows: `lab tdd`.)

Work on **one behaviour at a time**: two bugs are two rounds.

## Red

1. `./lab tdd red "<one-line behaviour>"`, for example `./lab tdd red "empty dashboard shows 0%"`.
2. Write **one** test for it in `tests/Tasks.App.Tests/`. Use `TestApp` (empty store, clock pinned)
   and `app.Clock.Set(...)` for a specific time. Test through the page (`client.GetAsync("/SomePage")`)
   or a method that **already exists**, so the test compiles against the code as it is.
3. Don't touch `src/` in this phase.

## Green

4. `./lab tdd green`. It runs the suite and moves to green only if a test **fails for the right
   reason**: an assertion, or the bug's exception. If the tests don't build, it refuses: a test that
   doesn't compile hasn't shown the bug. Fix the test, not the app.
5. Make the **smallest** change in `src/` that makes it pass. Don't edit the failing test.
6. `./lab tdd done`. It finishes only when the whole suite passes, including the test that failed.

## Next

7. Repeat from Red for the next behaviour.

## Final message

For each behaviour, give the test name and paste the `FAIL` line that `./lab tdd green` printed, in a
code block, unchanged. The reviewer wants to see the failure the test caught, not "tests pass".
