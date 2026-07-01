# Review Record - GameState ProductionOptionStates UnitSpec cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup GameState production options slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate gamestateproductionoptionsunitspec / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-production-options-unitspec.md`.
- Non-goals: changing old-runtime production click routing, changing production balance, deleting `GameState.ProductionDefinitions`, changing production queue snapshots, or changing production completion.

Implementation summary:
- Moved `GameState.ProductionOptionStates(...)` off legacy `ProductionDefinitions` enumeration.
- Added `ProductionSpecsFor(FactionId)` so old-runtime command-card options enumerate legacy `ProductionKind` values through `ProductionKindDesignBridge.LegacyProductionSpecs()`.
- Production button cost now comes from old-compatible `UnitSpec.Stats.Cost`; producer kind, category, lane, and duration come from legacy `ProductionSpec`; button presentation comes from `UnitPresentationCatalog.ForProductionSpec(...)`.
- Preserved old-runtime click compatibility by keeping `UnitDesignId` empty in `ProductionOptionState` until the legacy `ProductionKind` enqueue path is migrated.
- Added `ReviewGate gamestateproductionoptionsunitspec` to keep this GameState read path UnitSpec-backed.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: SimulationSmoke passed its 300 second smoke with 10 orders, 10 completions, 2 waves, and outcome InProgress.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, harvest/bank, T1-T3 production, rally, selection, move/attack/stance, victory and defeat.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateproductionoptionsunitspec`
  Result: pass
  Evidence: ReviewGate gamestateproductionoptionsunitspec completed with 0 errors and 0 warnings after narrowing the check to the ProductionOptionStates method body.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-production-options-unitspec`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic production-option metadata migration.
  Result: not run.
  Evidence: no rendering code changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: `GameState.ProductionDefinitions` remains used by legacy enqueue/cancel/queue/prod-completion compatibility paths until later slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes only the command-card option metadata read path, not the whole legacy production table.
