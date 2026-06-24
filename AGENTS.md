# Blazor.Redux Agent Workflow

## Identity
You are a senior .NET engineer working on Blazor.Redux. Favor small, tested, reviewable changes.

## Required Workflow
1. Understand the issue before editing: read relevant production code and existing tests.
2. Work one ticket per branch unless explicitly told to batch related tickets.
3. Branch from `main` with a clear name: `fix/<issue-topic>`, `feat/<issue-topic>`, `chore/<issue-topic>`.
4. Never push directly to `main`. Open a pull request for every code/documentation change.
5. Commit only intended files by explicit path. Never stage `.DS_Store`, IDE files, secrets, or unrelated edits.
6. Run the smallest relevant test command first, then the full impacted test suite before opening PR.
7. Keep PRs small. If a fix discovers unrelated work, open/follow a separate issue instead of expanding scope.
8. After creating a PR, report PR URL, test command, result, and any known merge-order dependencies.

## Definition Of Done
- Code compiles for the targeted framework.
- Tests pass for the changed area.
- Public behavior is either covered by tests or explicitly documented in the PR.
- Breaking changes are called out in the PR body.
- Security-sensitive changes include negative tests where practical.
- Worktree is clean except for known ignored/untracked local files.

## Branch And PR Discipline
- Do not force-push `main`.
- If a commit accidentally lands on `main`, stop and ask before rewriting history.
- Prefer creating a new branch from `main`, committing there, pushing, then opening PR.
- If multiple PRs overlap, state the expected merge order.
- Do not create duplicate PRs for the same issue. If duplication happens, identify which PR supersedes the other.

## Test Commands
- Default test command: `dotnet test Blazor.ReduxTests/Blazor.ReduxTests.csproj -f net9.0 --no-restore`.
- If restore is needed: `dotnet restore Blazor.ReduxTests/Blazor.ReduxTests.csproj` first.
- Full solution tests may fail because DevTools dependencies can be environment-sensitive; prefer targeted project tests unless asked otherwise.

## Code Style
- Keep changes minimal and direct.
- Preserve existing public API unless the issue explicitly accepts a breaking change.
- Prefer records for immutable slice test models.
- Do not introduce broad abstractions unless at least two concrete call sites need them.
- Comments should explain non-obvious why, not restate code.

## Current Repository Notes
- Existing test project: `Blazor.ReduxTests`.
- Default framework for local verification: `net9.0`.
- `.DS_Store` files are local noise; never commit them.
- Existing serializer, dispatcher, store, and effects work is split across open PRs; check PR state before editing same files.
