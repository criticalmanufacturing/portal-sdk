---
name: implement-work-item
description: 'Use when asked to implement an Azure DevOps User Story or Bug by ID, end to end — fetch the work item, implement the change, validate, and open a draft PR on GitHub.'
argument-hint: '<work-item-id>'
---

# Implement a work item end to end

The work item ID is this skill's argument — ask for it if absent.

1. **Fetch** — get the work item's full details with the Azure DevOps MCP tools, as defined in the `ado-work-items` skill. You implement from them.
2. **Branch** — create the branch following the `branch-pr-conventions` skill, with the work item ID before the slug.
3. **Implement** — before touching any C#, load any relevant development skills. Scope to the work item details fetched in step 1 and any user-provided context.
4. **Validate** — focused build and test, with tests in the right project.
5. **Commit** — commit your changes locally following the commit conventions.
6. **Draft PR**:
   - PR title - follow the commit conventions, do not include the work item id here
   - Description - Write it yourself; what was done, the work item it implements, dependencies,
   which tests ran or why they didn't, and any breaking change to public API, configuration, or
   behaviour.
   - **Show the user the PR title and description and
   wait for confirmation.** Then push the branch and create the PR with the `gh` CLI, following the
   `branch-pr-conventions` skill, with the confirmed title and description.
