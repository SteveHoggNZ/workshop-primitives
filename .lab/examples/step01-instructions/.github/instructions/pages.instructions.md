---
applyTo: "src/Tasks.App/Pages/**/*.cshtml"
description: "House conventions for Razor page templates"
---

# Page templates

The older pages (Index, Recent) predate these conventions. New pages follow them.

- Wrap the page's content in `<section data-page="...">` with the route name in lower case, for example `<section data-page="completed">`.
- Each task in a list is `<li data-testid="task-row" data-task-id="@task.Id">`.
- When a list is empty, render `<partial name="_Empty" />` instead of the list.
- Set `ViewData["Title"]` and an `<h1>` with the same text.
