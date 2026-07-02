# Review Record - M9 Building Target Id Buffers

Step:
M9 building target id scan buffer reuse (#105)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.BuildingProjection.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.SelectionPicking.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.EntityWorldSystems.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.BuildingSync.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProductionRallySelection.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.SyncRuntime.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.VisibilityCombat.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandBridge.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.TurretCombat.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandApplyRemoval.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: no building behavior, selection semantics, production rules, visibility rules, combat tuning, HUD layout, or unit/resource projection buffer changes.

Implementation summary:
- Added reusable building-id buffers to `UnitBattlefield` for primary, secondary, projection, and nested visibility scans.
- Replaced the allocating `BuildingTargetIds()` snapshot helper with `CollectBuildingTargetIds(List<int> result)`.
- Routed update, sync, placement, production candidate, rally, visibility, turret-combat, construction-subject, removal, and projection entry points through caller-owned building-id buffers.
- Kept building-id ordering stable with in-place `CompareBuildingIds` sorting.
- Extended `ReviewGate simhot` to require the reusable buffers and forbid the old `BuildingTargetIds()` / `new List<int>()` / `new HashSet<int>()` helper pattern.
- Synced validation-tool source budget evidence after adding ReviewGate assertions.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj`
  Result: pass
  Evidence: Selection stress passed 100 cases.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, production, rally, selection, movement, combat, victory, and defeat coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Building-id scan buffer reuse only; rendering and layout were not changed.

Reviewer result:
- Status: pass
- Required fixes: none identified in building-id scan paths.
- Residual risks: building projection result buffers and unit/resource projection result buffers remain separate follow-up slices (#104/#103).

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: building projection result buffers, unit/resource projection result buffers, and broader profiler-guided allocation cleanup.
- Reason: This closes only the reusable building target id scan child slice.
