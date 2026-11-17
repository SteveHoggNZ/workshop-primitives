# Tasks lab (.NET)

A small task-tracking web app (ASP.NET Core Razor Pages) used to practise coding-agent primitives:
instructions, skills, custom agents, hooks, quality gates and spec-driven development.

## Prerequisites

- .NET SDK **10.0.100 or later** (`dotnet --list-sdks`). Older SDKs are refused by `global.json`.
- Git, and VS Code with the C# extension (`ms-dotnettools.csharp`).
- GitHub Copilot in VS Code (agent mode) or the Copilot CLI.

## Quick start

```bash
./lab doctor                          # Windows: lab doctor
./lab warm                            # one-off: first build and the skill scripts' packages
dotnet test
dotnet run --project src/Tasks.App    # then open the URL it prints
./lab lint
```

## Lab steps

```bash
./lab list            # the steps, and which are installed
./lab reset           # throw away a Before (or After) run's changes
./lab apply 0         # install step 0's files as their own commit
./lab goto 3          # catch up: install every step before 3
```

| Step | Primitive |
|---|---|
| 0 | Warm-up: a linter the agent can read (VS Code) |
| 1 | Project instructions (`AGENTS.md`) and a scoped rule |
| 2 | A skill with a tested script, and a prompt file |
| 3 | A custom agent with its own tool list |
| 4 | A skill backed by a lab command |
| 5 | A skill with a script, for a cheaper model |
| 6 | Permissions, then hooks |
| 7 | A hook at the end of the session |
| 8 | Clarify before you build: three ways |

Each step: run the Before prompt in a new agent session, `./lab reset`, `./lab apply N`, read what
was installed, then run the same prompt in another new session and compare.

Settings come from environment variables: `APP_TZ` (default `Pacific/Auckland`), `APP_NOW` (pin the
clock, ISO 8601), `APP_SEED=false` (start with no tasks), `APP_PORT`.
