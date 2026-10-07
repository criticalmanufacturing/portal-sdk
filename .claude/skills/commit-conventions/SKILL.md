---
name: commit-conventions
description: 'Commit message format for this repository. Use whenever writing or amending a commit message, or drafting a change summary that becomes one.'
user-invocable: false
---

# Commit conventions

[Conventional Commits](https://www.conventionalcommits.org/): `<type>: <summary>`, optionally
`<type>(<scope>): <summary>`.

- Types: `feat`, `fix`, `chore`, `test`, `docs`, `refactor`, `perf`, `ci`
- Summary in the imperative, lowercase, no trailing period — "add app token public key override",
  not "Added ..."
- Scope, when it helps, is the project or area touched (`feat(packages):`, `fix(api):`)
- Body only when the *why* isn't obvious from the summary, but keep it short and focused
- Breaking changes: `!` after the type/scope (`feat(api)!:`)
