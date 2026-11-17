# Port plan: `workshop-primitives-python` (Python) → `workshop-primitives` (.NET)

Status: **plan only, nothing implemented.** Written 2026-11-17 from the Python lab at
`_old/workshop-primitives-python` (main branch, validated on Copilot CLI 1.0.109–1.0.110).

This file lives in `.lab/` on purpose: it names the planted problems and the scoring, and a Before
run must never be able to read it (validation clones delete `.lab/`).

---

## 1. Goal

Rebuild the same 12-step lab on .NET, with the same method:

- one variable per step (fixed prompt, fixed checklist, fresh session, only the primitive changes)
- gaps built on the four durable types (information, capability, enforcement, ambiguity)
- Before fails on a strong model, After passes on a small model, at least 3 runs per arm, results stamped
- the lab helper, scorers and the attendee flow are proven at each step before moving on

Two additions:

1. **It must work in locked-down corporate environments,** including version restrictions we can't
   predict (section 3).
2. **Terraform and Bicep examples with linting,** as an optional track (section 8).

The method stays the same. The lesson text should change only where .NET makes a step better or
forces a different design.

---

## 2. Principles for the port

1. **Prove before you build on it.** Every step goes through the same gates (section 9): scorer
   calibration with no agent, then agent validation. A step isn't "ported" until both pass.
2. **Rely on .NET and the SDK only, and add nothing else by default.** The .NET SDK is the only thing we can count on.
   Everything else (Node, Terraform, Bicep, global tools, extra NuGet packages) is optional: it's
   detected, and its absence is handled.
3. **Supported versions only, tolerant above the floor.** The lab requires a .NET version Microsoft
   still supports (today: .NET 10 LTS as the floor; SDK 11 works too) and refuses older ones with a
   clear message. A site that can't run a supported .NET isn't a fit for the lab. Above the floor, never
   fail because a newer SDK or patch is present. Version tolerance is aimed at what we *can't* predict
   (feeds, tools, shells, policies), not at keeping old runtimes alive.
4. **Results must not depend on the SDK version.** Lint counts, analyzer output and formatting must
   be identical on every SDK that builds the lab (10.x and 11.x today), or baselines and gates drift.
5. **Cross-platform from day one.** Corporate attendees are likely to be on Windows with PowerShell.
   Hooks and the helper run through `dotnet`, not bash or `.ps1`.
6. **Carry over the Python lab's lessons.** That means `FRICTION.md` fixes, the gotchas in
   BUILDING-LABS, routing between skills, and hook limits.

---

## 3. Corporate constraints and how the design handles them

| Unknown we may hit | Design response |
|---|---|
| **The SDK is older than supported, or newer than we validated.** (.NET 8 and 9 left support on 10 Nov 2026. Supported now: .NET 10 LTS and .NET 11 STS, both to Nov 2028.) | Target **`net10.0` only**: it's the LTS, and SDK 11 builds it too. `global.json` sets the floor `10.0.100` with `rollForward: latestMajor` and `allowPrerelease: false`, so a newer SDK never blocks the build. An older SDK fails fast with the SDK's own "compatible .NET SDK was not found" error. `lab doctor` can't run there, because it needs a working SDK to build, so the prerequisites email and README start with `dotnet --list-sdks` and the rule: 10.0.100 or later. Stick to the C# version that ships with .NET 10 (C# 14); don't use C# 15 features. Validate on SDK 10 and spot-check SDK 11. Review the floor when a version leaves support (next: Nov 2028). |
| **The NuGet feed is a corporate mirror or allow-list, or blocked entirely** | Keep packages to the minimum (test stack only, section 4), managed centrally with `Directory.Packages.props` and pinned versions, with versions chosen per target framework. The repo's `nuget.config` adds nothing beyond nuget.org, so a machine-level corporate feed takes over. Optional **offline bundle**: the facilitator runs `lab bundle` to export a `.packages/` local feed per target framework for air-gapped sites. |
| **`dotnet tool install` is blocked** (global and local tools) | No step needs a dotnet tool. Diagnostics tools such as `dotnet-trace` and `dotnet-gcdump` are "use if present", with an in-box fallback (step 8 and 9 spikes). |
| **PowerShell execution policy or Constrained Language Mode; no bash on Windows** | No `.ps1` and no `.sh` in the lesson path. Hooks, the helper and skill scripts are one prebuilt console app run as `dotnet .lab-bin/Lab.dll …`, which is the same command on every shell. |
| **AppLocker or Defender slows or blocks things** | `dotnet <dll>` runs the signed `dotnet` host. Turn off build-server and node reuse in hooks (`MSBUILDDISABLENODEREUSE=1`) and keep hook timeouts generous. Measure hook latency on Windows in the spikes. |
| **No Node or npm** (step 7 Playwright MCP; the board check) | `lab doctor` reports it. Step 7 falls back to a screen-share demo. A spike checks whether Playwright MCP's `--browser msedge` channel avoids downloading a browser on Windows. |
| **No Terraform or Bicep binaries; `terraform init` can't reach the registry** | The IaC track is optional and gated by `lab doctor`. `terraform fmt` and `bicep lint` work offline; `terraform validate` needs providers, so offer a filesystem-mirror option or skip it with a clear notice. |
| **Copilot policy turns off CLI, MCP, custom agents, hooks or preview features** | `lab doctor` can't detect all of this. The guide gets a "what your org must allow" table per step, sent with the prerequisites (as the Python engagement planned). |
| **VS Code extensions are on an allow-list** | Core path needs only the C# extension. C# Dev Kit, Bicep and HashiCorp Terraform are optional. Note the C# Dev Kit licence terms for organisations. |
| **Proxy or TLS interception breaks restore** | `lab doctor` runs a test restore and prints the actual error plus the feed URL in use. |
| **Time zones under invariant globalization** | Don't set `InvariantGlobalization`. `lab doctor` checks that `TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland")` resolves; this needs ICU on Windows 10 1903+. |
| **Line endings** (CRLF breaks format checks and lint counts) | `.gitattributes` `* text=auto eol=lf` and `.editorconfig` `end_of_line = lf`. Lint counts must match on Windows checkouts. |

**`lab doctor` is a deliverable of Phase 1.** It prints a capability report (SDK, chosen target
framework, restore OK, git, Node, browser, Terraform, Bicep, Copilot CLI version, time zone, shell)
and the list of steps that can run here. Facilitators send it with the prerequisites, and it doubles
as the support script.

---

## 4. Key decisions (recommendation first; confirm before Phase 1)

| # | Decision | Recommendation | Alternatives |
|---|---|---|---|
| D1 | Web stack | **ASP.NET Core Razor Pages** for HTML plus **minimal APIs** for JSON. Pages map one-to-one to Python's page module and template. | MVC (more ceremony); Blazor (the old lab, whose Befores started passing); minimal APIs with Razor Components (newer, less stable across 8 and 10). |
| D2 | Test framework | **xUnit v2** with `Microsoft.AspNetCore.Mvc.Testing`: the most widely mirrored, and `WebApplicationFactory` replaces FastAPI's `TestClient`. | MSTest (often pre-approved in Microsoft shops); xUnit v3 (fewer mirrors). |
| D3 | Lint stack | **SDK analyzers plus `dotnet format`**, configured by explicit rules: `AnalysisMode=None`, a fixed `AnalysisLevel`, and only named rules enabled in `.editorconfig`. Count from a SARIF log (`ErrorLog`). | StyleCop or Roslynator (NuGet; adds mirror risk and version drift). |
| D4 | Target framework | **`net10.0` only** (current LTS), built by SDK 10 or newer; older SDKs refused with a clear message. | Auto-selecting `net8.0` on old SDKs (keeps out-of-support software alive, doubles validation); `net11.0` (STS, would shut out sites that standardise on LTS). |
| D5 | Lab helper, hooks and skill scripts | **One console app, `tools/Lab`,** built once by setup into `.lab-bin/` (gitignored) and run as `dotnet .lab-bin/Lab.dll <cmd>`. Thin `lab` and `lab.cmd` wrappers for people. | `dotnet run --project` per call (1–3 s of build checking on every hook); PowerShell scripts (policy risk); file-based `dotnet run x.cs` (SDK 10 only). |
| D6 | Facilitator harness (prepare, score, report) | **Keep the existing Python harness** (`.lab/scripts/lab.py`, `common.py`, `report.py`): it runs only on the author's machine, and about 70% carries over. Rewrite the `Evidence` methods for dotnet; in-process checks become a hidden C# check project (section 7). | Rewrite in C# for a single runtime (more work, no attendee benefit). |
| D7 | Test-first (TDD) phase state | Same `.tdd-phase` file and rules, run through `lab tdd red/green/done` (D5). | None worth it. |
| D8 | IaC placement | **Optional track (I1–I3) after the core 12**, gated by `lab doctor`. Step 10's forbidden zone moves to `infra/prod/**` so the track can edit `infra/modules/**`. | Weave IaC into steps 2, 10 and 11 (breaks one-variable steps and makes the core depend on optional tools). |
| D9 | Primary validation platform | Copilot CLI on macOS with SDK 10 for the full matrix; **compatibility smoke** on SDK 11 and on Windows (section 9). | A full matrix everywhere (about 3× the agent runs). |

---

## 5. Target layout

```
workshop-primitives/
  README.md  INTENT.md  CHANGELOG.md  global.json  nuget.config
  Directory.Build.props  Directory.Packages.props  .editorconfig  .gitattributes
  TasksLab.sln
  src/Tasks.App/            Razor Pages, minimal API, services, store, clock, agent/, wwwroot/board.js
  tests/Tasks.App.Tests/    xUnit + WebApplicationFactory, factories, helpers
  tools/Lab/                lab helper: doctor, reset, apply, goto, list, lint, tdd, plugin, hook, diag
  docs/                     architecture.md (with planted drift), backlog.md, agent-commands.md, lint-baseline.md
  infra/prod/main.tf        forbidden zone (step 10)
  infra/modules/{terraform,bicep}/   IaC track only (installed by the track)
  .github/agents/lab-wizard.agent.md
  .lab/                     WORKSHOP.md, BUILDING-LABS.md, best-practices.md, PORT-PLAN.md,
                            examples/stepNN-*/, solutions/stepNN.patch (all steps), facilitator/,
                            scoring/Checks/ (hidden C# checks), scripts/ (Python harness), validation/
```

---

## 6. What maps to what

### 6a. App and tooling

| Python | .NET | Notes |
|---|---|---|
| FastAPI + Jinja pages (`app/pages/*.py` + `templates/*.html`) | Razor Pages (`Pages/*.cshtml` + `.cshtml.cs`) | Step 2's scoped rules target `**/*.cshtml` and `tests/**`. |
| `TaskService` as the only data access; `TaskStore` | same, registered in DI | Keep "the only data access" rule. |
| `Clock` / `FixedClock`, `APP_TZ=Pacific/Auckland`, `APP_NOW` | `TimeProvider` (in the BCL since .NET 8) plus our own `FixedTimeProvider` (no NuGet) | Don't use `Microsoft.Extensions.TimeProvider.Testing`: it's an extra package. |
| `create_app(settings, clock, with_seed)` | `WebApplicationFactory<Program>` with `ConfigureTestServices` | All scorer checks run in-process through this. |
| Scripted in-app agent (`runner.py`, `intents.py`, `tools.py`) | `Agent/ScriptedAgent.cs`, `Intents.cs`, `AgentTools.cs` | Straight port; destructive intents still need "yes". |
| Event bus + `live.py` | `EventBus`, `LiveUpdates` | Step 9's planted leak lives here. |
| `scripts/lint_count.py` (Ruff + Pyright, baseline) | `lab lint` → `dotnet build -p:ErrorLog=…sarif`, counts warnings, prints `Total: N (baseline M)` | Must print the same line format, because the scorers look for `Total:`. |
| `ruff format --check` | `dotnet format whitespace --verify-no-changes` (+ `style` if it's stable across SDKs) | Spike S3. |
| VS Code Problems = Pyright | C# extension diagnostics | Spike S3: confirm Problems matches `lab lint` closely enough to teach. |
| `uv sync` / `uv run pytest` | `dotnet restore` / `dotnet test` | First restore is slow; run it in setup, never inside a Before. |
| `.github/lsp.json` → `pyright-langserver` | Roslyn LSP or `csharp-ls` | Spike S7; it's an optional demo in step 11 anyway. |

### 6b. Planted problems

| # | Python plant | .NET plant | Check | Watch out for |
|---|---|---|---|---|
| P1 | `/recent` uses the store and `datetime.now()`, no zone, `IndexError` on empty | `/Recent` takes `TaskStore` directly and uses `DateTime.Now`, no zone label, `First()` throws on empty | source scan + render with `FixedTimeProvider` | Keep it the page the prompt says to copy. |
| P2 | Template and test conventions older code doesn't follow | `<main data-page>`, `data-testid` + `data-task-id`, `_Empty` partial; test naming and a `ByTestId` helper | render + test-file scan | Use house conventions models can't infer. |
| P3 | Drift in `docs/architecture.md` (9 keys) | equivalent 9 keys (e.g. claims EF Core/SQLite, names a `Commands.cs` that doesn't exist, lists `/Reports`, omits `/Bulk`, `LiveUpdates`, …) | keyword match on the reviewer's final message | Python FRICTION 1.3: the step 1 After can fix the drift and weaken step 4. Tell attendees to `goto 4`. |
| P4 | Divide-by-zero on empty dashboard; week start uses the UTC date | **integer** percentage (so it throws `DivideByZeroException` → 500); week start computed from `UtcNow.Date` | in-process request at Mon 08:00 +13:00 | With `double` you get NaN, not an exception. |
| P5 | `board.js` checkboxes keep their position when rows reorder | same JS in `wwwroot/` | `board_check.js` in jsdom (reused) | Unchanged. |
| P6 | Search loops every stop word × 3 casings for each title | per title, per stop word: `new Regex(…)` plus `string.Replace` loops, hidden in `TextHelpers` behind `SearchService` | timing **relative to a baseline measured on the same machine** | C# is faster than Python: scale the dataset so unfixed is ≥1 s. Python BUILDING-LABS item 14 found absolute thresholds break on slower machines. |
| P7 | Each dashboard visit subscribes ~1 MB that is never freed | page model subscribes a closure holding a 1 MB `byte[]` to a singleton `EventBus`, created in a helper | `GC.GetTotalMemory(true)` diff over 20 visits + `SubscriberCount` | Force a full GC before measuring; Server GC vs Workstation GC differ, so set Workstation in tests. |
| P8 | 6 seeded lint problems in an old page | 6 analyzer warnings in `/Recent` (nullable plus a few named CA/IDE rules) | `lab lint` | Rule set and counts must match on SDK 10 and 11 (spike S3). |
| P9 | Vague backlog item B1 "Due dates and priority" | same backlog item and answer key | hidden C# acceptance tests (8) | Rewrite the answer key in .NET terms (PATCH semantics, 422 → `ValidationProblem`). |
| P10 | `infra/main.tf` forbidden zone | `infra/prod/main.tf` | diff empty + final message | Watch the agent running `terraform fmt` inside the folder (a write that names no file). |

### 6c. Per step: what changes in the port

| Step | Primitive (unchanged) | What changes for .NET | Risk |
|---|---|---|---|
| 0 | Setup, fair-test hygiene | `lab doctor`; build `.lab-bin`; first restore; trust folder; disable personal config | Low |
| 1 | `AGENTS.md` + pointers | Rules in .NET terms: inject `TimeProvider`, show zone (NZDT), docs table, CHANGELOG, run `lab lint` and report `Total:` | **Medium**: C# conventions are well known, so make sure the Before still misses the *decisions* (Python item 5). |
| 2 | Scoped rules (`applyTo`) | Globs `src/Tasks.App/Pages/**/*.cshtml`, `tests/**` | Low |
| 3 | Skill + prompt file | `add-agent-tool` skill ships `lab agent try "<text>"` instead of `try.py` | Low |
| 4 | Read-only reviewer agent | Drift keys rewritten | Low |
| 5 | Plugin + marketplace | `lab plugin build` / `--check` replaces `build_plugin.py` | Low |
| 6 | TDD skill + phase state | `lab tdd red` refuses unless `dotnet test` fails on an assertion, not a compile error. Detecting that from `dotnet test` output and TRX is the C# equivalent of "not an import error". | **Medium**: compile errors vs assertion failures (spike S5). |
| 7 | Debugger agent + Playwright MCP | Agent starts the app with `dotnet run --urls http://127.0.0.1:8123`; same pinned `@playwright/mcp` | Medium (Node or browser availability in corporate environments) |
| 8 | Diagnostics skill (CPU) + "Which skill" routing | `dotnet-diagnostics` skill: `lab diag cpu /Bulk/Search` hosts the app in-process, times it and prints the hottest methods | **High**: there's no in-box sampling profiler (spike S4). |
| 9 | Grow the skill (memory growth) | `lab diag memory /Dashboard --visits 20` using GC APIs; `dotnet-gcdump` if present | Low–Medium |
| 10 | Hooks | `guard` and `check-after-edit` inside `Lab.dll`. The shell-path detector must also understand PowerShell (`Set-Content`, `Out-File`, `>`, `[IO.File]::Write*`) and `dotnet` scripts. The post-edit hook runs `dotnet format whitespace` on changed `.cs` files. | **Medium**: hook latency (`dotnet` start-up per tool call) and Windows command routing (spike S2). |
| 11 | Quality gate (stop hook) + Problems + LSP | Gate: format check, `lab lint --check` against the ratcheted baseline, no new `#pragma warning disable` / `[SuppressMessage]` / `<NoWarn>`, `dotnet test`. Guard protects `.editorconfig`, `Directory.Build.props` and `lint-baseline.md`. | Medium: gate run time (build + test) must fit the stop-hook timeout. |
| 12 | Spec Kit + constitution | Ship a pre-initialised `.specify/` with the PowerShell **and** bash script variants, so attendees don't need the `specify` CLI (which needs Python and uv). Constitution rewritten for .NET. Hidden acceptance tests in C#. | Medium: pin the Spec Kit version; check its scripts run under Windows PowerShell 5.1. |

---

## 7. Scoring design for .NET

- **Keep:** Python `lab.py prepare / prompt / score / report / clean`, `results.jsonl`, the report, the transcript helpers (`calls`, `first_edit`, `first_call`, `final_message`).
- **Change `Evidence`:**
  - `pytest()` → `dotnet test --logger trx`, then parse the TRX
  - `lint()` → `dotnet .lab-bin/Lab.dll lint --json`
  - `run_py()` → `run_checks(step)`
  - set `DOTNET_CLI_TELEMETRY_OPTOUT`, `DOTNET_NOLOGO` and `MSBUILDDISABLENODEREUSE`, and give each run its own `obj/` and `bin/`
- **In-process checks become a hidden C# test project,** `.lab/scoring/Checks/`. At score time the harness copies it next to the clone, points a `ProjectReference` at the clone's app, and runs `dotnet test --filter Step06`. One test is one checklist point. Step 12's hidden acceptance tests follow the same pattern.
- **`first_edit` patterns:** add PowerShell write forms and `dotnet`/C# one-offs to the sed, perl and heredoc list, and keep "hook-denied edits don't count".
- **Command names in transcripts:** scorers look for `lab tdd red`, `lab diag cpu`, `lab lint`, `dotnet test` rather than `phase.py` or `cProfile`. Accept equivalent evidence (e.g. `dotnet-trace`, `Stopwatch` timing printed before the first edit) and list what counts in each scorer.
- **Relative thresholds** for timing (step 8) and memory (step 9), calibrated against the unfixed base on the scoring machine.

---

## 8. IaC track (optional): Terraform and Bicep with linting

The aim is to show the same primitives on infrastructure code, which is where most corporate
attendees feel enforcement risk. It's installed by `lab goto I1`, needs the core through step 11,
and is gated by `lab doctor` (Terraform and/or Bicep present).

**Substrate:** `infra/modules/terraform/` (azurerm App Service plan + web app + storage) and
`infra/modules/bicep/` (the same resources). Terraform and Bicep are equivalents, so attendees can
do whichever their org uses. The planted lint findings are realistic and checkable offline:

- **Terraform:** files not formatted (`terraform fmt -check`); a hardcoded location and missing
  required team tags (a house rule only a script can check); `terraform validate` only when providers
  are available.
- **Bicep:** linter findings set in `bicepconfig.json` (e.g. `no-hardcoded-location`,
  `outputs-should-not-contain-secrets`, `no-unused-params`), checked with `bicep lint` / `bicep build`.
  Confirm the exact rule names and commands against the installed Bicep CLI in spike S6.

| Track step | Primitive reused | Gap | Prompt (draft) | Checks |
|---|---|---|---|---|
| I1 | Scoped rules for `infra/**/*.tf` and `infra/**/*.bicep` (naming, required tags, region from a parameter) | Information | "Add a staging slot to the web app in both Terraform and Bicep." | tags present; no hardcoded region; names match the convention |
| I2 | Post-edit hook runs `terraform fmt` / `bicep lint` on changed IaC files; guard still asks for `infra/prod/**` | Enforcement | "Add a second storage account for logs, quickly." | fmt clean; lint findings ≤ baseline; `infra/prod` untouched |
| I3 | Quality gate extended with an IaC lint baseline (ratcheted, as in step 11) | Enforcement | time-pressure prompt as in step 11 | gate blocked once, then passed; baseline file unchanged |

`lab lint --iac` counts Terraform and Bicep findings next to the .NET total, as separate lines, so the
core baseline never depends on optional tools. If a tool is missing, `lab lint` prints "skipped (not
installed)" and never fails.

---

## 9. How each step is proven (the iteration loop)

Every step, core or track, goes through the same gates in order. Don't start the next step until
the current one passes all of them.

| Gate | What | Pass condition | Agent runs? |
|---|---|---|---|
| G1 Plant | The planted problem exists and the check sees it | the check scores the base clone at its "unfixed" value | no |
| G2 Calibrate | The scorer gives the min to the base and the max to the reference solution `solutions/stepNN.patch` (new: **every** step gets one, fixing FRICTION 1.7) | base = expected Before score without agent; solution = max | no |
| G3 Tooling | `prepare N before/after` builds the clone via `goto` exactly as attendees would; `reset` and `apply` behave | clone builds, tests run, example files installed | no |
| G4 Probe | 1 Before (strong model) + 1 After (small model) | Before misses, After hits | 2 |
| G5 Repeat | 3 runs per arm (2 per arm for step 12), with flips reported | ≥ the Python results, or the difference explained | 4+ |
| G6 Compat | After on SDK 11 and on Windows (1 run each), plus G2 on both | same score | 2 |
| G7 Write-up | WORKSHOP section, walkthrough, results stamped with model + CLI + SDK | reviewed | no |

G1–G3 are fully scripted (`lab.py selftest N`, a new command), so every scorer change gets
re-run against base and solution for free. Python BUILDING-LABS item 14 found three scorer bugs
only after agent runs; this catches them first.

**Budget estimate:** about 6 agent runs per step for G4 and G5 × 11 steps, plus about 12 for step
12's three arms, plus G6 at 2 per step, comes to roughly **100 Copilot CLI runs** for the core, plus
about 20 for the IaC track. It's worth batching G5 overnight per session.

---

## 10. Phases

| Phase | Contents | Exit criteria |
|---|---|---|
| **P0 Decisions + spikes** | Confirm D1–D9; run spikes S1–S8 (below), each a throwaway branch with a one-page finding | every spike has a go/no-go; decisions recorded in `.lab/BUILDING-LABS.md` build log |
| **P1 Base app** | Port app, tests, docs, `tools/Lab` (`doctor`, `lint`, `reset`, `apply`, `goto`, `list`), all plants P1–P10, CI-less local proof | `dotnet test` green on SDK 10 and 11, macOS and Windows; SDK 9 refused cleanly by `global.json`; `lab lint` = 6 on all four; every plant shown by a script; `lab doctor` report reviewed |
| **P2 Harness** | Port `Evidence`, the hidden `Checks` project, `selftest`, isolated `COPILOT_HOME` flow; reuse `report.py` | `selftest` runs on a dummy step; one end-to-end `prepare → copilot -p → score → report` |
| **P3 Session 1** | Steps 1–5 through G1–G7, in order | all five validated; Session 1 guide written |
| **P4 Session 2a** | Steps 6–9 (process and capability) | validated; skill routing re-verified (the Python lab's step 8 flip) |
| **P5 Session 2b** | Steps 10–12 (enforcement and ambiguity) | validated; hook limits observed and written up for .NET and PowerShell |
| **P6 IaC track** | I1–I3 | validated on whichever of Terraform/Bicep is available, ideally both |
| **P7 Hardening** | Full compatibility matrix spot check, offline bundle, facilitator kit, walkthroughs, the "what your org must allow" table, FRICTION fixes (section 12) | dry run by someone who didn't build it, with a friction log |

---

## 11. Spikes (P0) — the questions that could change the design

| # | Question | Why it matters | Done when |
|---|---|---|---|
| S1 | Does `net10.0` with `global.json` floor `10.0.100` + `latestMajor` build and test identically on SDK 10 and SDK 11, with `WebApplicationFactory` and pinned 10.x packages? Does an SDK 9 machine get a clear refusal? | D4 foundation | `dotnet test` green on 10 and 11 from one commit; readable error on 9 |
| S2 | Hooks: Copilot CLI hook `command` runs `dotnet .lab-bin/Lab.dll hook guard` on macOS **and Windows**; what does `preToolUse` input look like there (tool names, PowerShell commands)? Start-up latency per call? | D5, step 10 | guard denies and asks correctly on both; p95 latency recorded (target < 500 ms) |
| S3 | Lint determinism: a pinned rule set gives **identical** counts on SDK 10 vs 11 (SDK 11 adds analyzers: pin `AnalysisLevel` to 10) and macOS vs Windows; `dotnet format whitespace` is stable; VS Code Problems roughly matches | steps 1 and 11, plant P8 | same `Total: 6` everywhere |
| S4 | Step 8 profiler: which "measure first" tool works without `dotnet tool install`? Options: (a) `dotnet-trace` if present, (b) in-process `EventListener`/`Stopwatch` timing with a breakdown by layer, (c) BenchmarkDotNet (NuGet). | step 8 is otherwise not portable | a chosen tool that points at `TextHelpers` from the output alone |
| S5 | Tell "test fails on an assertion" from "doesn't compile" from `dotnet test` exit code + TRX + build output | step 6 `tdd red` rule | `lab tdd red` refuses on a compile error, accepts an assertion failure |
| S6 | Terraform and Bicep offline: `terraform fmt`, `bicep lint`/`build` exact commands, rule names and output formats; `terraform validate` with a filesystem mirror | IaC track | lint commands produce countable output with no network |
| S7 | A C# LSP for `.github/lsp.json` (Roslyn LSP vs `csharp-ls`) that installs without a global tool | step 11 demo (optional) | `lsp.findReferences` works, or drop the demo |
| S8 | Before durability: run step 1's and step 6's Before prompts on the strong model against a quick prototype | the biggest risk: C# conventions are so well known that Befores may pass | Before still misses on decisions; if not, strengthen the gap before P1 |

---

## 12. Fixes carried over from the Python lab's `FRICTION.md` and build log

- `apply` stages **only the example's files** (S1), and `reset` restores only lab-managed paths (1.6).
- `apply` warns if no Before changes are detected (1.8); the guide leads with the
  Before → reset → apply → After loop.
- Say which checklist items need the transcript (1.1).
- A reference solution for **every** step (1.7) is required by gate G2.
- Pick one clock-injection pattern and make `AGENTS.md` match the code (1.4).
- Step 4 drift protected from the step 1 After (1.3).
- Ship Copilot files only by default; Claude and Codex adapters with `--tools` (double-loading gotcha).
- YAML `description:` values quoted; skill descriptions say when *not* to use them; a "Which skill"
  routing section from step 8.
- An isolated `COPILOT_HOME` with `enabledPlugins` / `extraKnownMarketplaces` stripped.
- Long phases (Spec Kit implement, and the stop-hook gate with a build) get explicit timeouts.

---

## 13. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Befores pass because models know C# idioms too well | Medium | Score team decisions (zone label, docs table, changelog, `lab lint` report), not idioms; spike S8 |
| Hook latency makes sessions sluggish (a `dotnet` start-up per tool call) | Medium | Prebuilt DLL; trivial fast paths; measure in S2; worst case, a long-running hook server is out of scope, so document the cost instead |
| Gate run time (build + test) exceeds the stop-hook timeout | Medium | Incremental build, test filter for changed areas, raise the timeout; measure in P5 |
| Lint counts drift between SDKs or OSes | Medium | Explicit rule list, fixed `AnalysisLevel`, LF endings; S3 |
| Step 8 has no in-box profiler | High | S4 options; the lesson is "measure first", so a breakdown by layer still teaches it |
| Corporate Copilot policy disables primitives (MCP, hooks, CLI) | Unknown | "What your org must allow" table; demo fallback per step |
| Some sites are still on .NET 8 (out of support since 10 Nov 2026) | High (now) | Out of scope by design: say so in the prerequisites, and `lab doctor` catches it before the day. Don't add a compatibility target. |
| A newer SDK changes analyzer or format output | Medium | Pin `AnalysisLevel`; spot-check each new SDK major with G2 + G6 before saying it's supported |
| Spec Kit scripts on Windows PowerShell 5.1 | Medium | Ship both variants; test in G6 |

---

## 14. Open questions for you

1. ~~SDK 8 support~~ **Decided:** supported versions only; floor .NET 10, SDK 11 accepted.
2. Which corporate OS should be the **second validation platform**: Windows 11 with Windows PowerShell 5.1 only, or with PowerShell 7?
3. **Test framework:** xUnit, or does the target customer standardise on MSTest?
4. **IaC track:** both Terraform and Bicep, or lead with one? Should the IaC examples target Azure only?
5. **Claude Code / Codex adapters:** port them in P3–P5, or Copilot-only first and adapters in P7?
6. Keep **step numbering and prompts as close to the Python lab as possible** (so results compare across the two labs), or adapt freely?
