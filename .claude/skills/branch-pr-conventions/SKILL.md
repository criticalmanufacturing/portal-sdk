---
name: branch-pr-conventions
description: 'Branch naming and pull request conventions for the portal-sdk GitHub repository. Use before creating a branch or opening a pull request.'
user-invocable: false
---

# Branch & PR conventions

## Scope

The repository is `criticalmanufacturing/portal-sdk` on GitHub. Use the `gh` CLI for the GitHub steps;
check its `--help` when unsure of the current syntax rather than guessing.
Work items live in Azure DevOps and are fetched as defined in the `ado-work-items` skill.

## Branch naming

[Conventional Branch](https://conventionalbranch.org/): `<type>/<description>`

```
<type>/{work-item-id}-{slug}   # working on a work item
<type>/{slug}                  # otherwise
```

`main` is the base branch. Unless otherwise specified, all new branches should be created off an
up-to-date `main`.

- `<type>` is one of:
  - `feat/` — new feature (User Story)
  - `fix/` — bug fix (Bug)
  - `hotfix/` — urgent fix
  - `release/` — release preparation (`release/v1.20.0`)
  - `chore/` — non-code tasks: dependencies, docs, tooling
- Don't use the agent source prefixes (`ai/`, `claude/`, `codex/`, `copilot/`, `cursor/`) — the type
  describes the work, not who wrote it
- `{slug}` is a lowercase hyphenated short form of the work item title, dropping a leading verb that
  repeats the type
- Only `a-z`, `0-9`, hyphens and dots (dots only in versions). No uppercase, underscores or spaces;
  no consecutive, leading or trailing hyphens or dots

Examples:

- US 12345 "Add support for async await" → `feat/12345-add-support-for-async-await`
- Bug 9876 "Fix null reference on startup" → `fix/9876-null-reference-on-startup`
- No work item, "Update agent skills" → `chore/update-agent-skills`

Push the new branch and set its remote as upstream.

## Pull requests

- **Always draft.** Never open a ready-for-review PR and never promote one, even if the user omits
  the word "draft"
- Base branch `main` unless otherwise specified; source branch is the current one, pushed
- Title follows the `commit-conventions` skill, without the work item ID
- **The title and description content come from whoever requested the PR** — they made the change,
  they know what it does. When a workflow skill has you draft them (e.g. `implement-work-item`), show
  the draft to the requester and create the PR only with the version they confirm. Never create a PR
  with content the requester hasn't supplied or confirmed
- Description body structure comes from the repository's GitHub pull request template when it has
  one; otherwise use these sections:
  - **Summary** — what the change does and why, and the work item it implements (ID and title) —
    GitHub can't link an Azure DevOps work item to the PR itself
  - **What's Changed** — the changes made, including whether anything breaks: public API
    signatures, configuration or behaviour
  - **Validation** — which builds and tests ran, or why they didn't
- Pass markdown bodies (descriptions, comments) from a file rather than inline, so shell quoting
  doesn't mangle them

## PR comments

- Post a comment only when asked to, and only with text the requester supplied or confirmed —
  verbatim, as a new PR comment. Never write, edit, summarise, or extend review content yourself

## Never

- Submit a review, approve, request changes on, merge, close, or mark ready a pull request
- Resolve or unresolve review threads
- Push to `main`, force-push, or bypass branch protection or required checks
- Create, touch, update, delete, or reassign work items unless explicitly asked
- Transition work item states, remove links, or change Area/Iteration Path
- Target any repository other than `criticalmanufacturing/portal-sdk`
