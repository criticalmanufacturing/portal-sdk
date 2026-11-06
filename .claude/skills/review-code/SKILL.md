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

## Severity

- **P1 — blocking** — breaks behaviour, a "Do Not Break" constraint, or security.
- **P2 — should-fix** — a correctness risk, a convention violation, or changed behaviour without
  appropriate tests, worth fixing in this change.
- **P3 — nit** — style or preference; never holds the change back once answered.

## Reviewing a pull request by number

When the argument is a PR number or URL, use the `gh` CLI for the GitHub steps, following the
`branch-pr-conventions` skill. Keep each finding on its own — an inline thread, or a PR comment
when it isn't tied to a line in the diff — so the author can answer it and the reviewer can resolve
it after verification.

1. **Fetch** — get the PR's title, description, and base and head branches. If the description
   references an Azure DevOps work item, fetch its details as defined in the `ado-work-items` skill.
   Get the diff with `git`, comparing the PR head against its merge base with the base branch. Fetch
   the head through the PR's own ref, since it may come from a fork. Get the review threads and PR
   comments, with their resolved state, through GitHub's GraphQL API.
2. **Verify your earlier findings** — check each of your open findings against HEAD and the replies:
   - Fixed, rejected with a reason you accept, or a P3 the author has answered → propose resolving it.
   - Answered with a blocker only the user can clear (access, an environment, a decision) that you
     agree with → keep it open, reply that it stays open until that's done, and count it as blocked.
     A new finding is never blocked, even when its blocker is already known: the author answers
     it first.
   - Rejected with a reason you don't accept → draft a counter-argument for it.
   - Not answered and not fixed → leave it as is.

   Track the IDs of threads and comments you opened across rounds. Only resolve those; if
   ownership cannot be verified, leave it unchanged. Never reopen a resolved thread.
3. **Review** — according to the scope and relevant skills and instructions. Skip anything an
   existing finding already covers, resolved or not.
4. **Publish** — resolve the verified threads you own and publish the findings and replies
   verbatim. Start each new finding with "# AI REVIEW" followed by its severity (e.g.
   `**P2 · should-fix**`). Reply inside a review thread when contesting an answer; for a PR
   comment, post a new one quoting (`>`) it. No new findings or thread updates → post nothing.

   Never approve, request changes, or change the PR's state.

Skip steps 2 and 4 if the user asked for a review without a PR number or URL.

## PR comment placement

- Publish **one finding per thread or comment**. When a finding has a meaningful location in the
  diff, post it inline on the relevant file and smallest useful line span; submit all inline
  findings as one review with no body (through `gh api`, with the JSON from a file). Do not
  combine findings in one PR comment.
- Use a separate **PR comment** for a finding without a meaningful anchor in the diff, such as a
  missing requirement spanning the change. Explain the evidence and consequence without inventing
  a code location.
- Keep the review summary, assumptions, and validation results in a PR comment when publishing
  them. Do not repeat findings already posted on their own.
- Verify anchors against the PR's current head commit before posting: inline comments must land on
  lines in the diff, and the posted comment must show the intended location.

## Reporting

- **Findings first**, ordered by severity, each labelled with its severity and explaining the
  failure it causes — not just the rule it breaks. Include a concrete file reference (`path:line`)
  where meaningful; otherwise identify it as a PR-level finding and give the supporting evidence.
- Summary brief and secondary, after the findings.
- State your assumptions when context is incomplete.
- Say which validation you ran (build, tests) and which you skipped.
- If nothing is wrong, say so plainly rather than padding with minor nits.
