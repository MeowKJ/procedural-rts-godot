# Review Record - M9 Building Projection Buffers

Step:
M9 building projection result buffer reuse (#104)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.BuildingProjection.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.SelectionPicking.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: no projection DTO shape changes, no HUD layout changes, no selection semantics changes, no rally semantics changes, no minimap exploration rule changes, and no unit/resource projection buffer changes.

Implementation summary:
- Added reusable result buffers for building snapshots, selected building rally projections, selected building selection projections, selected building entity ids, hit-pulse projections, and minimap projections.
- Replaced building projection `Where/Select/OrderBy/ToList/ToArray` materialization with explicit loops and in-place sorting.
- Preserved adjacent minimap snapshot comparisons with a two-buffer minimap projection pattern, so hidden/explored calls can be compared without allocating a new list per call.
- Extended `ReviewGate simhot` with compact building projection buffer checks while keeping `UnitBattlefieldAllocationReviewGate.cs` at the 200-line validation-source limit.
- Synced validation-tool source budget evidence after the ReviewGate changes.

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
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj`
  Result: pass
  Evidence: CombatBehavior passed production, minimap, economy, enemy AI, and outcome coverage after fixing adjacent minimap snapshot reuse.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Projection buffer reuse only; rendering and layout were not changed.

Reviewer result:
- Status: pass
- Required fixes: fixed minimap projection buffer reuse so adjacent hidden/explored calls do not overwrite each other.
- Residual risks: unit/resource projection result buffers remain a separate follow-up slice (#103).

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: unit/resource projection buffers and broader profiler-guided allocation cleanup.
- Reason: This closes only the building projection result buffer child slice.
