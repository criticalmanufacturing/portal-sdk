---
name: create-release
description: 'Use when asked to make, cut, or publish a portal-sdk release on GitHub — checks the version on main, creates the tag and publishes the release with GitHub-generated notes.'
argument-hint: '<version, e.g. 2.0.1 or 2.1.0-rc.1; defaults to the version in src/Version.props on main>'
---

# Create a release

Publishing a release runs `.github/workflows/npm-publish.yml`, which uploads the packages to the GitHub
release, Nexus and npm. An npm version can't be published twice, so a wrong release can't be fully undone:
check everything before publishing.

## Conventions

- **Version** — the `<Version>` in `src/Version.props` on `main`. The publish workflow reads the version from
  that file, not from the tag, so they must match. Release candidates are `X.Y.Z-rc.N`
- **Tag** — the bare version (`2.0.1`, `2.1.0-rc.1`), no `v` prefix: a lightweight tag on the `main` commit
  that holds that version
- **Title** — `Cmf.CustomerPortal.Sdk v<version>`
- **Pre-release** — a version with a pre-release suffix (`-rc.N`) is published as a pre-release (npm `next`);
  any other as a regular release (npm `latest`)
- **Notes** — always GitHub's auto-generated notes (`--generate-notes`). Never write, extend or edit them
- Published directly, never as a draft

## Steps

1. **Version** — `git fetch origin main`, take the `origin/main` commit sha and read the version from
   `git show origin/main:src/Version.props`. Use that sha for every later step. If a version was requested
   and it differs, stop: the version bump (`chore: bump version to <version>`) must be merged into `main` first.
2. **Tag must not exist** — check locally (`git tag -l <version>`), on the remote
   (`git ls-remote --tags origin refs/tags/<version>`) and as a release (`gh release view <version>`).
   If any of them exists, stop and report what you found.
3. **CI** — the `build.yml` run for that sha must have succeeded
   (`gh run list --workflow build.yml --commit <sha>`). If it failed, is still running or is missing, stop and report.
4. **Confirm** — show the version, commit (short sha and subject), tag, title and whether it is a pre-release,
   and wait for the user's explicit confirmation.
5. **Tag** — `git tag <version> <sha>`, then `git push origin refs/tags/<version>`.
6. **Release**:
   ```
   gh release create <version> --repo criticalmanufacturing/portal-sdk --verify-tag \
     --title "Cmf.CustomerPortal.Sdk v<version>" --generate-notes [--prerelease]
   ```
7. **Report** — the release URL and the `npm-publish.yml` run it started
   (`gh run list --workflow npm-publish.yml --limit 1`), then watch the run in the background
   (`gh run watch <run-id> --exit-status`). If it fails, follow the next section.

## If the publish run fails

1. **Find the failed step** — `gh run view <run-id> --json jobs` and `gh run view <run-id> --log-failed`.
   - **Transient artifact upload error** — a release asset upload or the Nexus upload failed on the network:
     a connection reset, timeout or broken stream (e.g. `curl: (92) HTTP/2 stream ... PROTOCOL_ERROR`), or an
     HTTP 5xx. Retry automatically, without asking (steps 2–4)
   - **Anything else** — a build or test step, an npm step, or an upload the server rejected (HTTP 4xx such as
     401, 403 or 400): stop and report, don't rerun. After an npm step failed, npm may already have the version,
     and it can't be published twice
2. **Delete the release attachments** — the asset upload steps fail when the file is already attached, so run
   `gh release delete-asset <version> <asset> --yes` for every asset in `gh release view <version> --json assets`.
   Stop if any are left.
3. **Rerun** — `gh run rerun <run-id>`, wait until the run's `attempt` (`gh run view <run-id> --json attempt`)
   goes up, then `gh run watch <run-id> --exit-status`.
4. **Repeat** — at most 5 reruns, waiting 5 minutes after each failed one. Check each failure with step 1 again
   and keep going only while it is a transient artifact upload error. Run the loop in the background, and report
   each failed attempt and the final result.
5. **Verify** — the release has its attachments again and the run succeeded. npm can take a few minutes to show
   a new version, so poll `npm view @criticalmanufacturing/portal dist-tags --prefer-online` for a while before
   reporting that it is missing.

## Never

- Release a commit that isn't on `main`
- Move, delete, force-push or reuse an existing tag, or edit or recreate an existing release
- Bump the version as part of the release — that is a separate PR merged first
- Rerun the publish workflow for anything other than a transient artifact upload error, or more than 5 times
