# Review Record - GameState UnitSpec spatial cleanup

Step: UnitSpec architecture phase 3 GameState spatial slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate gamestateunitspecspatial / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-unitspec-spatial.md`.
- Non-goals: changing unit balance, deleting `UnitDefinition`, migrating target-profile/damage/label compatibility paths, or rewriting movement algorithms.

Implementation summary:
- Added `UnitRuntimeDescriptorFor(...)` inside `GameState` as the local UnitSpec runtime-data read path.
- Selection overlap, group move formation data, attack-slot geometry, fog unit sources, picking, dynamic path obstacles, movement stepping, path assignment, local avoidance, production spawn geometry, harvester gather distance, death VFX payloads, and unit combat-source geometry now read unit radius/domain/sight/speed/turn/attack/weapon data from `UnitSpecRuntimeDescriptor`.
- Kept legacy `Definition(unit)` reads only for later target-profile, damage, and alert-label compatibility slices.
- Added `ReviewGate gamestateunitspecspatial` to prevent these unit self-data paths from regressing to direct legacy `UnitDefinition` reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors before final gate wiring.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateunitspecspatial`
  Result: pass
  Evidence: ReviewGate gamestateunitspecspatial completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-unitspec-spatial`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23 steps, including SimReplay, CombatBehavior, FogOfWarQa, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless QA.

Manual/visual gates:
- Check: visual inspection not required for this narrow runtime data-source migration.
  Result: not run.
  Evidence: formulas remain unchanged; only the source of unit runtime data moved to UnitSpec descriptors.

Reviewer result:
- Status: pass
- Required fixes: none known before gate execution.
- Residual risks: `GameState` still contains legacy `Definition(unit)` reads for unit target profiles, effective damage, weapon lookup compatibility, and alert labels. Those remain later cleanup slices.

TODO update:
- Items marked done: none, because the broad UnitSpec duplicate-data cleanup remains open.
- Items left open: UnitSpec architecture phase 3 duplicate-data cleanup.
- Reason: this completes one bounded read-path slice, not deletion of all legacy UnitKind/UnitDefinition compatibility.
