# Review Record - M9 Production Option State Scans

Step:
M9 production option state scan buffer reuse (#93)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.EntityWorldSystems.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProductionRally.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProductionOptions.cs`, `tools/ReviewGateDomains/UnitBattlefieldProductionAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: no production balance, production option count, HUD visual, hotkey, UnitDesign authoring, or queue summary changes.

Implementation summary:
- Split production option state calculation out of `UnitBattlefield.ProductionRally.cs` into `UnitBattlefield.ProductionOptions.cs`.
- Added `_productionDesignSpecBuffer` so UnitDesign option state generation reuses design spec storage.
- Reused `_productionCandidateProducerIds` for both legacy `ProductionKind` and UnitDesign option-state paths.
- Replaced per-option producer `ToList()` and queue `Sum/Select/Where/DefaultIfEmpty/Max` chains with explicit queue loops.
- Removed the old allocating `CandidateProducerIds(...)` and `ProductionDesignSpecs(...)` enumerable helpers.
- Extended `ReviewGate simhot` to lock producer candidate buffers, explicit queue metric loops, and the removal of the old enumerable helpers.
- Synced validation-tool source budget evidence after expanding ReviewGate domain checks.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 errors; one Godot dll copy retry warning resolved during the build.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj`
  Result: pass
  Evidence: PlayerLoopQa passed, including T1-T3 production, rally, selection, victory, and defeat coverage.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj`
  Result: pass
  Evidence: CombatBehavior passed, including rally production, production presentation descriptors, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after syncing validation-tool source budget evidence.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Option-state allocation refactor only; no rendering or HUD layout changed.

Reviewer result:
- Status: pass
- Required fixes: none identified in production option state paths.
- Residual risks: remaining allocation paydown opportunities exist in building projection/minimap and broader EntityWorldSystems placement helper paths.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: broader profiler-guided allocation cleanup outside production option/queue paths.
- Reason: This closes only the production option state allocation child slice.
