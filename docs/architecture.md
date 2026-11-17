# Architecture

Tasks is a small ASP.NET Core app: Razor Pages for the HTML, plus a JSON API.

## Data

Tasks are stored in SQLite through Entity Framework Core (`Data/TasksDbContext.cs`).
`TaskService` is the only code that reads or writes tasks; pages and the API go through it.

Time comes from an injected `TimeProvider`, so tests can pin the clock. The user's time zone is
`APP_TZ` (default `Pacific/Auckland`).

## Pages

| Route | Page | What it shows |
|---|---|---|
| `/` | Index | Open tasks and counts |
| `/Recent` | Recent | The five newest tasks |
| `/Reports` | Reports | Weekly completion report |
| `/Agent` | Agent | The in-app agent: type commands to manage tasks |

## API

| Endpoint | What it returns |
|---|---|
| `GET /api/tasks` | All tasks, newest first |

## Services

| Service | File | Job |
|---|---|---|
| `TaskService` | `Services/TaskService.cs` | The only data access |
| `TaskCommands` | `Services/TaskCommands.cs` | Parses quick-add commands |
| `ScriptedAgent` | `Agent/ScriptedAgent.cs` | The in-app agent: matches commands in `AgentIntents` to tools in `AgentTools` |
