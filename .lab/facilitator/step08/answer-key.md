# Step 8 answer key: B1 "Due dates and priority"

The facilitator plays the stakeholder. Answer **only what is asked**, in these words. Anything not
asked stays a guess; that's the point of the step. The hidden tests are `.lab/scripts/checks/LabCheck08.cs`.

| Topic | Decision |
|---|---|
| Priority values | `low`, `normal`, `high`, `urgent`. Default `normal`. |
| Due date | Optional calendar date, `YYYY-MM-DD`, no time of day. JSON field `dueDate` (camelCase, like the rest of the API). |
| API fields | Tasks gain `priority`, `dueDate` and a read-only boolean `overdue`, in every task the API returns. |
| Create / update | `POST /api/tasks` and `PATCH /api/tasks/{id}` accept `priority` and `dueDate`; `"dueDate": null` clears it. An unknown priority → 422. |
| List | `GET /api/tasks` returns all tasks, oldest first (creation order). |
| Sort | `GET /api/tasks?sort=priority`: urgent, high, normal, low. Ties: earlier due date first, tasks without a due date last, then oldest created first. |
| Filters | `?priority=<value>` exact match. `?due_before=YYYY-MM-DD` is **inclusive** and leaves out tasks with no due date. Results keep creation order. |
| Overdue | Not done, and due date **before today in the user's time zone** (`APP_TZ`, Pacific/Auckland). Due today is not overdue. Done tasks are never overdue. |
| Task list page `/` | Each task shows its priority word; overdue tasks show the word `Overdue`. |
