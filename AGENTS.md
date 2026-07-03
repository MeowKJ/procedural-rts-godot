# Codex Operating Entry

This is the first file Codex agents should read in this repository. Keep it
short. Do not replace it with long project history.

## Default Reading Budget

Read in this order:

1. This file.
2. The active GitHub issue body and its latest non-obsolete comments.
3. The issue context pack, if present.
4. Only the target files named by the issue or discovered by focused `rg`.

Do not read `docs/reviews/` or historical archive files by default. They are
evidence stores, not navigation entry points.

## Work Rules

- One bounded issue per branch unless the user explicitly asks for a batch.
- Branch names use `codex/issue-<number>-<slug>`.
- Owner agents implement. Reviewer agents review. Integrator agents run gates and
  merge. Do not silently switch roles inside one slice.
- Comment planned scope before coding when working from GitHub issues.
- Do not create a local backlog file. Active work belongs in GitHub Issues and
  the GitHub Project.
- Do not create new local process docs or slice evidence markdown. Put current
  status, review notes, and verification evidence in the GitHub issue, PR, or CI
  artifact.
- Do not close an issue until verification evidence exists.
- If a stop/pause note exists on the issue, do not claim or implement it.

## Gate Ladder

Use the smallest gate set that proves the slice, then let CI run the full gate.
Do not wait on GitHub Actions in an active Codex session unless the user asks.

1. Preflight: `dotnet build ProceduralRts.csproj --no-restore` and
   `git diff --check`.
2. Slice gate: the relevant replay, QA tool, or narrow `ReviewGate` mode.
3. Review gate: PR review plus the GitHub PR template verification fields.
4. Merge gate: `sh tools/verify-all.sh` or the GitHub `VerifyAll` workflow.

Async handoff: after a PR is pushed, record `CI: pending` and let
`VerifyAll PR Status` plus `tools/ci-monitor.sh` handle follow-up.

## Context Hygiene

- Prefer a 20-line issue context pack over broad repo exploration.
- Prefer exact commands and file references over narrative progress logs.
- Put durable evidence in the PR, issue, or CI artifact; keep comments short.
- Use `tools/ci-failure-tail.sh` for failed CI; never paste full logs into context.
- If you need history, open the one named archive file or issue link. Do
  not scan every historical record.
