# Review Record - CombatBehavior HasUnitDefinition cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup CombatBehavior HasUnitDefinition slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate combatbehaviorhasunitdefinitioncleanup / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-combatbehavior-hasunitdefinition-cleanup.md`.
- Non-goals: deleting `GameState.HasUnitDefinition(...)`, deleting legacy `UnitDefinition`, changing faction rosters, changing unit presentation, or changing balance data.

Implementation summary:
- Moved CombatBehavior faction roster coverage checks off `GameState.HasUnitDefinition(...)`.
- Moved CombatBehavior shared unit presentation coverage checks off `GameState.HasUnitDefinition(...)`.
- Both coverage checks now prove `UnitKind` coverage through `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)`.
- Added `ReviewGate combatbehaviorhasunitdefinitioncleanup` to prevent this tool read path from regressing.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorhasunitdefinitioncleanup`
  Result: pass
  Evidence: ReviewGate combatbehaviorhasunitdefinitioncleanup completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=combatbehavior-hasunitdefinition-cleanup`
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
  Evidence: no runtime visuals changed; roster and presentation assertions are unchanged.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: `GameState.HasUnitDefinition(...)` remains as a compatibility API until the broader legacy deletion milestone.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes CombatBehavior's direct presence checks against the legacy accessor, not all compatibility APIs.
