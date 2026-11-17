# In-app agent commands

What users can type at `/Agent`. Keep this table in step with `src/Tasks.App/Agent/AgentIntents.cs`.

| Command | Tool | Confirms first | Example |
|---|---|---|---|
| `add <title>` | `add_task` | no | `add buy milk` |
| `list` | `list_tasks` | no | `list` |
| `done <id>` | `complete_task` | no | `done seed3` |
| `delete <id>` | `delete_task` | yes | `delete seed3` |
