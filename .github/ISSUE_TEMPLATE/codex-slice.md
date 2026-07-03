---
name: Codex slice
about: Small bounded implementation slice with compact context
title: "[Slice] "
labels: "status:ready,size:S"
assignees: ""
---

## Goal

-

## Context pack

Target files:
-

Relevant docs:
- AGENTS.md

Non-goals:
-

Required gates:
- dotnet build ProceduralRts.csproj --no-restore
- git diff --check

Async CI:
- Open draft PR after narrow gates pass.
- Do not wait on VerifyAll; use `sh tools/ci-monitor.sh <pr-number>`.

Evidence destination:
- GitHub PR, issue comments, and CI artifacts only.

Known risks:
-

## Done

-
