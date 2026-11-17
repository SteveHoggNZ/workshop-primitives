# Drift key: docs/architecture.md vs the code (for the read-only reviewer step)

Planted on purpose. A reviewer that finds most of these, without editing anything, passes.

| # | The doc says | The code says | Match on |
|---|---|---|---|
| 1 | Data in SQLite via Entity Framework Core | In-memory `TaskStore` (`Data/TaskStore.cs`) | `sqlite`, `entity framework`, `ef core`, `in-memory`, `in memory` |
| 2 | `Data/TasksDbContext.cs` | No such file | `TasksDbContext` |
| 3 | A `/Reports` page | No such page | `/Reports`, `Reports` |
| 4 | `GET /api/tasks` | The endpoint is `GET /api/tasks/recent` | `/api/tasks/recent` |
| 5 | `Services/TaskCommands.cs` | No such file | `TaskCommands` |
| 6 | (missing) | `ActivityFeed` service | `ActivityFeed` |
| 7 | (missing) | `ITaskQueries` interface | `ITaskQueries` |

Notes:
- A Step 1 After that "tidies" the doc can fix some of these. Run `./lab reset` (or `./lab goto 3`) before the reviewer step.
- Keep this key in step with the code: when a later step adds pages or services, decide whether the doc should list them.
