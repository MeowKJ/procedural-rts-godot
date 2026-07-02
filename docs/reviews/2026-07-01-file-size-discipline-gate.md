# Review Record - File-size discipline guard

Step: M1 architecture file-size discipline
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate filesize / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/ReviewGate/Program.cs`, `tools/ReviewGate/ReviewGateRunner.cs`, `tools/ReviewGate/ReviewGateRegistry.cs`, `tools/ReviewGate/ReviewGateEvidence.cs`, `tools/ReviewGateCore/`, `tools/ReviewGateFileSize/`, `tools/ReviewGateDomains/`, split validation suites under `tools/`, `scripts/core/`, `tools/SelectionStress/SelectionStress.csproj`, `tools/DesktopHudQa/DesktopHudQa.csproj`, `tools/FogOfWarQa/Program.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Governance source: `docs/FileStructureGovernance.md`.
- Non-goals: immediately splitting every historical large file, changing gameplay behavior, or changing current M1 migration order.

Implementation summary:
- Split the old monolithic `tools/ReviewGate/Program.cs` into a tiny entry point, a runner, a registry, shared evidence helpers, specialized gates, and domain-organized check files under `tools/ReviewGate/checks/`.
- Added `ReviewGate filesize` as the FileStructureGovernance enforcement gate so the policy remains outside the runner and registry.
- Recorded the user's thresholds: < 200 healthy, 200-400 normal, 400-600 yellow, > 600 red, > 1000 debt.
- Removed the old red-line whitelist after the major god-file splits; untracked C# files over 600 lines now fail by default.
- Added stable-entrypoint, vague filename, directory crowding, same-prefix split-family, and Bridge/Legacy/Compatibility baseline checks from FileStructureGovernance.
- Added a stricter validation-system limit: every `tools/ReviewGate/**/*.cs` source file must stay at 200 lines or below after the healthy split.
- Converted the historical `ReviewGateChecks` partial aggregate into independent check classes named after their files, backed by explicit static imports.
- Split the long registry table into domain-owned registry entry files under `tools/ReviewGate/registry/`; `ReviewGateRegistry.cs` now only exposes lookup/run orchestration.
- Split the remaining 300-line check buckets into focused part classes under per-bucket subdirectories, keeping the directory crowding gate clean.
- Split `CoreChecks01` into argument parsing, project-root, TODO, review-record, text-assertion, and method-extraction responsibilities.
- Split `FileSizeGate` into entrypoint, policy, evidence, source catalog, threshold checks, and structure checks under `tools/ReviewGateFileSize/`.
- Added a self-check in `FileSizeGate` so ReviewGate fails if the historical `ReviewGateChecks` aggregate or partial check aggregates are recreated.
- Current validation-system line counts: `Program.cs` is a tiny entry point, `ReviewGateRegistry.cs` is under 40 lines. ReviewGate runner current source budget: 9 C# source files / 567 total lines; largest C# file tools/ReviewGate/ReviewGateEvidence.cs has 148 lines.
- Current all-tools suite budget lock: Validation tool suites current source budget: 130 C# source files / 17933 total lines across 53 suites; largest C# file tools/CombatBehaviorSkirmish/SkirmishAi.cs has 393 lines; largest suite tools/ReviewGateDomains has 600 lines.
- Follow-up suite cleanup split the former large `tools/SimReplay` and `tools/CombatBehavior` validation directories into named suites such as `SimReplayCombatTargets`, `SimReplayConstructionCore`, `CombatBehaviorProduction`, and `CombatBehaviorPresentation`; `FileSizeGate` now fails any validation tool suite over 1000 lines.
- Follow-up cleanup in this record added `FileSizeToolBudgetChecks`, global using files for `CombatBehavior` and `SimReplay`, reused existing SimReplay group/combat fixtures, and added the no-command deterministic replay overload.
- Current runtime file-size cleanup: the yellow watchlist is cleared; `ResourceSystem` max companion file is 125 lines.
- Current directory cleanup: `scripts/core` root now keeps only `GameState.cs`; former root files live under focused domain directories, and the largest source directory is below the file-count warning threshold.

Automated gates:
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings; yellow watchlist is empty and source-directory crowding is cleared.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, replay, ReviewGate, perf smoke, and Godot headless QA.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: future semantic cleanup after mechanical splits remains; directory/file-size gates are now clean.

TODO update:
- Items marked done: `File-size discipline guard`, `ReviewGate God-file split`, registry split, validation-system self-check, yellow-file cleanup sprint, and `scripts/core` domain-directory consolidation.
- Items left open: future semantic cleanup after the mechanical splits.
- Reason: the guard prevents new untracked red-line growth now; ReviewGate itself is split into independent classes and protected by the stricter validation-system ceiling.
