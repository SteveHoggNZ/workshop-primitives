# Tasks app constitution

Non-negotiable rules every spec, plan and task list is checked against. Short on purpose.

## I. One way to the data

`TaskService` is the only data access. Pages, API endpoints and agent tools never use `TaskStore` directly.

## II. Time is injected and local where it matters

Get the time from the injected `TimeProvider`, never `DateTime.Now`. Store times in UTC. Anything that
depends on "today" or "this week" uses the user's time zone (`AppSettings.Zone`, from `APP_TZ`).

## III. Test-first, one behaviour at a time

Every acceptance criterion has at least one test written before the code that satisfies it (the `tdd`
skill). Tests use `TestApp` and its pinned clock.

## IV. Small, reviewable steps

Each task changes as little as possible and leaves `dotnet test` green. No speculative features.

## V. The quality bar holds

Finish only with the quality gate green: lint at or below the baseline, tests passing. Never suppress a
warning or move the baseline.

## VI. Ask when it matters

When a requirement can reasonably mean two things, ask. Record the answer in the spec. Never invent a
public contract: API field names and accepted values, query parameters, filter and sort semantics
(including ties and edge cases), validation responses, and what derived states such as "overdue" mean
are the stakeholder's decisions. Mark each one `[NEEDS CLARIFICATION]` in the spec until it is answered.

**Version**: 1.1.0 | **Ratified**: 2026-11-18
