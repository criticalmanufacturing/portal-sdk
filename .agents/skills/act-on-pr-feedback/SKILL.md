---
name: act-on-pr-feedback
description: 'Use when asked to address the feedback left on a pull request.'
argument-hint: '<pr-number>'
user-invocable: true
---

# Act on PR feedback

Every open comment and reply on the pull request must end up acted upon: either fixed in code, or
answered with a reason it wasn't. Never leave one unaccounted for in the report.

## Scope

The basis for addressing feedback is the PR branch's current HEAD plus its comments and replies —
not a re-review of the diff against the base branch.

"Do Not Break" constraints outrank anything stylistic when deciding
whether a comment is valid.

Load any relevant development skills before addressing the feedback.

## Addressing

Use the `gh` CLI for the GitHub steps, following the `branch-pr-conventions` skill.

1. **Fetch** — get the PR's head branch, base branch, title and description, and all its feedback.
   GitHub has three kinds:
   - **Inline review threads** — the only kind with a resolved state, which GitHub exposes through
     its GraphQL API, not the REST one. Drop resolved threads before triage — they get no code
     change and no reply. Outdated threads are not dropped; triage them against HEAD like any other.
   - **Conversation comments** on the PR.
   - **Review summaries** — the body of a submitted review. Skip reviews with an empty body.

   Fetch (and check out, if you need to build/run/grep it) the head branch's HEAD with `git`.
2. **Triage** every remaining thread and comment against the branch HEAD:
   - **Not valid** (already handled, misunderstanding, out of scope, conflicts with an
     architecture constraint, etc.) — no code change. Draft a reply explaining why.
   - **Valid** — make the code change that addresses it. Draft a reply stating what
     changed (`path:line`).
3. **Report to the user first**, in the format below.
4. **Post** — show the user the exact reply text for each thread and **wait for confirmation**.
   Then post each reply verbatim:
   - Review thread — reply inside the thread, not as a new PR comment.
   - Conversation comment or review summary — GitHub can't thread replies to these; post a new PR
     comment that quotes (`>`) the comment it answers.

   **Never** resolve a thread, submit a review, approve, request changes, or otherwise change the
   state of any comment or the PR itself — only reply text is posted.

## Reporting

- **One entry per triaged thread or comment**, listing: the comment, your verdict (addressed / not
  valid), and either the concrete change (`path:line`) or the reason it wasn't valid.
- State your assumptions when context is incomplete.
- Say which validation you ran (build, tests) and which you skipped.
