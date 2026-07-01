# Review Record - CombatBehavior UnitSpec read-path cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup CombatBehavior remaining read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate combatbehaviorunitspecreadpath / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-combatbehavior-unitspec-readpath.md`.
- Non-goals: deleting legacy `GameState.UnitDefinitionFor(...)`, deleting legacy `UnitDefinition`, changing combat behavior, changing pathing behavior, or changing balance data.

Implementation summary:
- Replaced the remaining direct `GameState.UnitDefinitionFor(...)` reads in CombatBehavior.
- Default tank / infantry / harvester metadata now comes from `RuntimeDescriptorFor(...)` / `UnitSpecRuntimeDescriptor`.
- Faction tier QA now reads `TechTier` from UnitSpec runtime descriptors.
- Generic legacy compatibility QA now projects explicit descriptor-backed compatibility definitions through `UnitDesignDefinitionCatalog.CompatibilityDefinition(...)`.
- Entity-attacked label assertions now compare against the UnitSpec runtime descriptor label.
- Added `ReviewGate combatbehaviorunitspecreadpath` so CombatBehavior cannot regress to direct legacy unit-definition reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorunitspecreadpath`
  Result: pass
  Evidence: ReviewGate combatbehaviorunitspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=combatbehavior-unitspec-readpath`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic tool read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed; CombatBehavior scenario assertions are unchanged.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: legacy `GameState.UnitDefinitionFor(...)` remains in `GameState` itself as a compatibility API for old runtime paths until the broader legacy deletion milestone.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes direct CombatBehavior reads of the legacy accessor, not all legacy `UnitKind` / `UnitDefinition` compatibility surfaces.
