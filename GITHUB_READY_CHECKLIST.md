# GitHub Readiness Checklist

## Completed in This Repository

- [x] Git initialized and default branch set to `main`
- [x] Added `.gitignore` for .NET/PowerShell/build artifacts/log outputs
- [x] Added `.gitattributes` for clean diffs and line endings
- [x] Added `.editorconfig` for baseline consistency
- [x] Added `LICENSE` (MIT)
- [x] Added `CONTRIBUTING.md`
- [x] Added `CODE_OF_CONDUCT.md`
- [x] Added `SECURITY.md`
- [x] Added `CHANGELOG.md`
- [x] Added GitHub issue templates
- [x] Added pull request template
- [x] Added CI workflow for restore/build/script syntax checks
- [x] Added Dependabot config
- [x] Upgraded root `README.md`

## Required Before Public Launch

- [ ] Replace placeholder security contact in `SECURITY.md`
- [ ] Replace issue template security URL placeholder (`OWNER/REPO`) in `.github/ISSUE_TEMPLATE/config.yml`
- [ ] Review and redact any sensitive file paths, sample data, or host-specific values
- [ ] Decide if this repository should be `Public` or `Private`
- [ ] Add repository description, topics, and website URL in GitHub settings
- [ ] Enable branch protection for `main` (require PR + status checks)

## Recommended GitHub Settings

- [ ] Enable vulnerability alerts (Dependabot alerts)
- [ ] Enable Dependabot security updates
- [ ] Require pull request reviews before merge
- [ ] Require CI status checks before merge
- [ ] Enable auto-delete for merged branches
- [ ] Configure default squash-merge strategy (optional)

## Exact Branch Protection Setup (main)

Use GitHub web UI:

1. Open repository Settings.
2. Open Rules, then Rulesets.
3. Click New ruleset and choose New branch ruleset.
4. Name: `Protect main`.
5. Enforcement status: `Active`.
6. Target branches:
	- Add target by pattern: `main`

Enable these rules:

- [ ] Restrict deletions
- [ ] Restrict force pushes
- [ ] Require a pull request before merging
- [ ] Require approvals: set to `1` minimum
- [ ] Dismiss stale pull request approvals when new commits are pushed
- [ ] Require approval of the most recent reviewable push
- [ ] Require status checks to pass before merging
- [ ] Require branches to be up to date before merging
- [ ] Status checks: add the CI job check shown by this repo workflow (`build` from workflow `CI`)
- [ ] Require conversation resolution before merging
- [ ] Block merge queue bypass (if merge queue is enabled)

Optional hardening:

- [ ] Require signed commits
- [ ] Require linear history
- [ ] Require deployments to succeed before merging (if you add environments later)

Admin and bypass recommendations:

- [ ] Do not add bypass actors initially
- [ ] Apply rules to administrators

Validation after saving ruleset:

- [ ] Open a test branch and create a pull request into `main`
- [ ] Confirm direct push to `main` is blocked
- [ ] Confirm merge is blocked until CI check `build` passes
- [ ] Confirm at least one approval is required

## First Publish Commands

Run from repository root after creating an empty GitHub repository:

```powershell
git add .
git commit -m "chore: prepare repository for GitHub"
git remote add origin https://github.com/<your-user-or-org>/<your-repo>.git
git push -u origin main
```

If `origin` already exists:

```powershell
git remote set-url origin https://github.com/<your-user-or-org>/<your-repo>.git
git push -u origin main
```
