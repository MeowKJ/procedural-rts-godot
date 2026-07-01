# Review Record - CombatBehavior Unit fixture UnitSpec read-path cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup CombatBehavior fixture read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate combatbehaviorunitfixturereadpath / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-combatbehavior-unit-fixture-readpath.md`.
- Non-goals: deleting legacy `GameState.UnitDefinitionFor(...)`, migrating tier/combat target QA reads, changing combat behavior, changing pathing behavior, or changing balance data.

Implementation summary:
- Added a narrow `RuntimeDescriptorFor(UnitKind kind)` helper in CombatBehavior.
- CombatBehavior `Unit(...)` fixture HP now reads from `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)` / `UnitSpecRuntimeDescriptor.MaxHp`.
- CombatBehavior path-anchor and firing-anchor radius assertions now read from `UnitSpecRuntimeDescriptor.Radius`.
- Added `ReviewGate combatbehaviorunitfixturereadpath` to prevent these tool read paths from regressing.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorunitfixturereadpath`
  Result: pass
  Evidence: ReviewGate combatbehaviorunitfixturereadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=combatbehavior-unit-fixture-readpath`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic tool fixture read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed; CombatBehavior assertions and scenarios are unchanged.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: CombatBehavior still keeps other legacy `GameState.UnitDefinitionFor(...)` reads for later tier/combat metadata compatibility slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one scoped CombatBehavior fixture/radius read-path cleanup, not full legacy `UnitKind` deletion.
