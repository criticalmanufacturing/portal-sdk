---
name: ado-work-items
description: 'How to query and report Azure DevOps work items (User Stories and Bugs) — query scope, current iteration, and which fields to fetch. Use before fetching a work item or querying the backlog.'
user-invocable: false
---

# ADO work items

## Query scope

- Default project `Product`, types User Story and Bug, states New and Active.
- **Resolve the team's current iteration via MCP before any sprint-scoped query** — never ask for it,
  never hardcode it.
- Don't query outside that area path unless asked.

## Work item fields

For every work item, always fetch and report ID, type, state, title, assignee and Target Version.

When implementing, reviewing, or otherwise working on a work item, also fetch its detail fields —
never work from the summary row alone:

- **User Story** — Details (`System.Description`) and Discussion (comments).
- **Bug** — Repro Steps (`Microsoft.VSTS.TCM.ReproSteps`), System Info
  (`Microsoft.VSTS.TCM.SystemInfo`) and Discussion (comments).

Discussion is not a work item field: fetch it with the MCP comments tool. Render the HTML fields as
readable markdown and say "(empty)" for any that has no content.
