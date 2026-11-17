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

Each step: run the Before prompt in a new agent session, `./lab reset`, `./lab apply N`, read what
was installed, then run the same prompt in another new session and compare.

Settings come from environment variables: `APP_TZ` (default `Pacific/Auckland`), `APP_NOW` (pin the
clock, ISO 8601), `APP_SEED=false` (start with no tasks), `APP_PORT`.
