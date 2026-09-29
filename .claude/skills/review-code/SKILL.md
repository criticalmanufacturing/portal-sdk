---
name: review-code
description: 'Use when asked to review code changes — a diff, a branch, a pull request, or a file. Review only; no implementation.'
argument-hint: '<what to review: branch, PR number, file, or nothing for the working diff>'
user-invocable: true
---

# Review Code

Review only. Report findings — **do not** implement the fixes.

## Scope

Review what the user named. With no argument, review the working diff against the base branch.
"Do Not Break" constraints are the highest-severity class of finding, ahead of anything stylistic.

## Reviewing a pull request by number

When the argument is a PR number or URL, use the `gh` CLI for the GitHub steps, following the
`branch-pr-conventions` skill.

1. **Fetch** — get the PR's title, description, and base and head branches. If the description
   references an Azure DevOps work item, fetch its details as defined in the `ado-work-items` skill.
   Get the diff with `git`, comparing the PR head against its merge base with the base branch. Fetch
   the head through the PR's own ref, since it may come from a fork.
2. **Review** — as above.
3. **Report to the user first**, in the format below.
4. **Post** — show the user the exact comment text you intend to publish and **wait for
   confirmation**. Then post it on the PR as a new comment, verbatim, prepending "# AI REVIEW".
   Never submit a review, approve, request changes, or otherwise change the PR's state.

Skip step 4 if the user asked for a review without a PR number.

## Reporting

- **Findings first**, ordered by severity, each with a concrete file reference (`path:line`) and the
  failure it causes — not just the rule it breaks.
- Summary brief and secondary, after the findings.
- State your assumptions when context is incomplete.
- Say which validation you ran (build, tests) and which you skipped.
- If nothing is wrong, say so plainly rather than padding with minor nits.
