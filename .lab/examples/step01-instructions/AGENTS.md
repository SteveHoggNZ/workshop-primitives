# Tasks app: notes for coding agents

## Stack

- ASP.NET Core Razor Pages on .NET 10 (`net10.0`), C# 14. In-memory data, no database.
- Tests: xUnit with `WebApplicationFactory` in `tests/Tasks.App.Tests`.

## Layout

- `src/Tasks.App/Pages/`: one Razor Page per route (`Name.cshtml` + `Name.cshtml.cs`); layout and partials in `Pages/Shared/`.
- `src/Tasks.App/Services/`: `TaskService` (the only data access), `ITaskQueries`, `ActivityFeed`, `FixedTimeProvider`.
- `src/Tasks.App/Data/`: `TaskStore` (storage, used only by `TaskService`) and `SeedData`.
- `tests/Tasks.App.Tests/`: `TestApp` starts the app empty with the clock pinned to Mon 16 Nov 2026 10:00 in Auckland.
- `docs/architecture.md`: the Pages, API and Services tables. `CHANGELOG.md`: what changed.

## Rules

- Read and change tasks only through `TaskService` (or `ITaskQueries` for read-only code). Never use `TaskStore` outside `TaskService`.
- Get the time from the injected `TimeProvider`. Never `DateTime.Now` or `DateTime.UtcNow`.
- Show user-facing times in the user's zone (`AppSettings.Zone`, from `APP_TZ`) as `HH:mm` and the UTC offset,
  for example `10:00 +13:00`: `TimeZoneInfo.ConvertTime(now, settings.Zone).ToString("HH:mm zzz", CultureInfo.InvariantCulture)`.
- Pages that list tasks handle an empty list.
- Page templates follow `.github/instructions/pages.instructions.md` (applied automatically to `.cshtml` files).

## Definition of done

- `dotnet test` passes, with a test for the new behaviour.
- A new page is linked from the nav in `Pages/Shared/_Layout.cshtml` and listed in the Pages table in `docs/architecture.md`.
- A line under `## Unreleased` in `CHANGELOG.md`.
- Run `./lab lint` and quote its `Total:` line in your final message.

## Commands

- `dotnet test` in the terminal: it takes seconds. Don't use the editor's test runner, which rebuilds everything and takes about a minute a run.
- `dotnet run --project src/Tasks.App`
- `./lab lint` (Windows: `lab lint`)
