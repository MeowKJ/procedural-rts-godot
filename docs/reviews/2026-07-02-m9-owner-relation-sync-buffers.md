# Review Record - M9 Owner Relation Sync Buffers

Step:
M9 owner relation sync buffer reuse (#95)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandBridge.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no `PlayerRelationTable`, owner/faction hostility, AI planner, match setup, UI, or visual changes.

Implementation summary:
- Added `_ownerRelationSlots` as reusable storage for owner relation synchronization.
- Replaced the `SyncOwnerRelations()` `Select/Concat/Where/Distinct/OrderBy/ToList` chain with explicit unit, building-identity, inventory, and fixed-slot scans.
- Preserved stable ordering through in-place `PlayerSlotId` sorting.
- Added `ReviewGate simhot` evidence forbidding the old chained slot materialization.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including shared threat propagation, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed, including construction, economy, production, movement/attack/stance, victory, and defeat coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Command bridge allocation refactor only; no rendering or UI changed.

Reviewer result:
- Status: pass
- Required fixes: none after moving the new ReviewGate checks into their own suite.
- Residual risks: `EntityWorldSystems` still has a separate owner/building concat path outside this issue's `SyncOwnerRelations()` scope.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: broader runtime and EntityWorldSystems allocation cleanup.
- Reason: This closes only the owner relation sync allocation child slice.
