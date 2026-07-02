# Review Record - M9 Unit Resource Projection Buffers

Step:
M9 unit and resource projection buffer reuse (#103)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CoreQueries.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.VisibilityCombat.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: no fog/visibility rule changes, resource economy changes, selection rule changes, HUD rendering changes, or `BattleRoot` / `HudLayer` copy behavior changes.

Implementation summary:
- Added reusable buffers for unit projections, vision sources, resource pips, unit minimap pips, and unit selection summary rows.
- Replaced `UnitProjections()`, `VisionSources(...)`, `ResourcePips(...)`, `MinimapPips(...)`, and `SelectionSummary()` LINQ materialization with explicit loops and in-place sorting/counting.
- Used two reusable buffers for resource pips and unit minimap pips so adjacent snapshot comparisons do not overwrite each other.
- Replaced the remaining live-unit LINQ filter in `MarkVisibleBuildingFootprints()` with an explicit loop.
- Folded #103 ReviewGate checks into `UnitBattlefieldAllocationReviewGate` while compacting existing source, reducing `ReviewGateDomains` from 997 to 970 lines.
- Synced validation-tool source budget evidence after the ReviewGate source compaction.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj`
  Result: pass
  Evidence: CombatBehavior passed presentation descriptors, resource/vision snapshots, minimap pips, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj`
  Result: pass
  Evidence: Selection stress passed 100 cases.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, production, rally, selection, movement, combat, victory, and defeat coverage.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj`
  Result: pass
  Evidence: Fog-of-war QA passed mask, explored memory, hidden enemy, camera-scoped texture, and 100-source smoke checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Readout buffer reuse only; rendering and layout were not changed.

Reviewer result:
- Status: pass
- Required fixes: none after adding adjacent-snapshot buffers for resource and minimap pip readouts.
- Residual risks: broader profiler-guided allocation cleanup remains open under #10/#58.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: broader profiler-guided allocation cleanup.
- Reason: This closes only the unit/resource projection buffer child slice.
