---
name: deliver-work-item
description: 'Use when asked to deliver an Azure DevOps User Story or Bug by ID with agents — a developer implements it and opens a draft PR, reviewers review it, and the developer addresses their feedback until the reviews are clean.'
argument-hint: '<work-item-id>'
---

# Deliver a work item

The work item ID is this skill's argument — ask for it if absent. Explicitly invoking this workflow
authorizes implementation, pushing its branch, creating its draft PR, publishing review findings
and replies, and marking that PR ready after successful review. Use the `gh` CLI for the GitHub
steps and follow the `ado-work-items` and `branch-pr-conventions` skills. Do not change work item
state.

You orchestrate: you never write code, review it, or post findings or replies yourself. The PR's
threads and comments are how the agents hand work to each other. Continue without routine
confirmation; if an agent needs information it cannot reasonably assume or an action lacks
permission, finalize and report what needs the user.

Spawn each agent once and retain its ID for the whole run. Use the current runtime's resume or
message tool to start its next round (Claude: `SendMessage`; Codex: its agent follow-up/resume
tool). If resuming is refused, spawn a replacement of the same type and pass its predecessor's
worktree path and owned review thread IDs so it can continue safely.

Every prompt says which skill to follow, and the work item ID — plus the PR number once there is
one. Let the agents' skills guide their work.

## Agents

- `developer` — implements the work item and addresses feedback.
- Reviewers — every configured `reviewer*` agent type. Run all of them in each review round, in
  parallel when the runtime supports it.

## Flow

1. **Implement** — spawn `developer` to follow `implement-work-item`. It reports the PR number and
   head branch. No PR → Finalize.
2. **Review** — spawn every reviewer in the first round, then resume them in later rounds to
   follow `review-code` on the PR. Each reports its count of open findings per severity, and how
   many of them are blocked on the user.
3. **Decide**:
   - No reviewer has open findings → Finalize. Open P3s still get a developer round, so each is
     fixed or answered before its reviewer resolves it.
   - Every open finding was answered in a developer round and is blocked on the user → Finalize.
   - The developer has already addressed feedback 3 times → Finalize.
   - Otherwise resume `developer` to follow `act-on-pr-feedback`, then return to Review.

   Every developer round is followed by a review round, even one that changed no code, so the
   reviewers see its replies. A developer that answers a finding it can't fix without the user
   has finished its round. An agent that fails (e.g. build broken or push rejected) → Finalize.
4. **Finalize**:
   - A PR exists, every configured reviewer completed a review of its latest head, all are clean,
     and no agent failed → mark that PR ready for review with `gh pr ready`.
   - Otherwise leave the PR as a draft; report any open findings or incomplete work.
   - Stop agent activity before cleanup. Remove only the worktrees created for this run with
     `git worktree remove`, after confirming their work is pushed and no uncommitted work remains.
     Never force removal; if git refuses, leave the worktree and report its path.

## Report

- PR link, review round count, whether it was marked ready, and why it remained a draft if so.
- Per reviewer: findings resolved, open, and blocked on the user.
- Validation, assumptions, unanswered questions, failures, and any retained worktrees.
