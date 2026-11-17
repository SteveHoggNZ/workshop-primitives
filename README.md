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

Settings come from environment variables: `APP_TZ` (default `Pacific/Auckland`), `APP_NOW` (pin the
clock, ISO 8601), `APP_SEED=false` (start with no tasks), `APP_PORT`.
