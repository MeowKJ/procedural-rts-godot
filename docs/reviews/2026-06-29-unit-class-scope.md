# Review Record - Unit class scope

Step: Guard the vertical-slice unit classes and keep naval units paper-only.
Milestone: Playable 1v1 Skirmish - scope locks.
Owner AI: Codex.
Reviewer AI: Codex self-review (CombatBehavior and ReviewGate provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/UnitKind.cs`, `scripts/core/UnitCatalog.cs`, `scripts/core/UnitSpec.cs`, `scripts/core/units/UnitRoleTag.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-unit-class-scope.md`.
- Non-goals: no new unit content, no full per-faction air roster, no naval implementation, no claim that all classes are fully trainable from production yet.

Implementation summary:
- Added CombatBehavior checks proving legacy units include light infantry-style units, land vehicle/tank units, aircraft, and land harvesters.
- Added CombatBehavior checks proving playable legacy unit definitions do not include naval/amphibious/ship units.
- Added CombatBehavior checks proving `UnitDesign` specs cover infantry, vehicle, aircraft, and economy/worker harvester roles.
- Added CombatBehavior checks proving `UnitDesign` specs do not include playable naval/amphibious/ship units.
- Added `ReviewGate unitclasses`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including unit-class and no-naval scope checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitclasses --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for unit class scope.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this is scope/data validation; class-specific visual polish remains under art/style TODOs.

Reviewer result:
- Status: pass
- Required fixes: initial test referenced `StatsSpec.Armor`; corrected to `StatsSpec.ArmorTag`.
- Residual risks: Dog/Cat full faction completeness and T1-T3 trainability remain open; this slice only proves the class boundaries and no playable naval content.

TODO update:
- Items marked done: `Unit classes this slice: Light (infantry-style), Tank (vehicle), Aircraft, plus Harvester (economy). Ships/Naval are designed on paper but NOT built this slice.`
- Items left open: Dog/Cat fully playable, T1/T2/T3 roster per faction, player can train T1-T3 from producers, no naval built remains also listed in out-of-scope guardrails.
- Reason: automated tests now prove the implemented unit classes are present and naval/ship units remain out of playable definitions.
