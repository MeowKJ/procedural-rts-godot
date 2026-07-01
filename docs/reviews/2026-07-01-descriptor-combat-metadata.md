# Review Record - Descriptor-only combat metadata cleanup

Step:
- Descriptor-only combat metadata cleanup

Milestone:
- M1 EntityWorld authority / UnitSpec duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex plus read-only explorer subagent; ReviewGate descriptorcombatmetadata

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/WeaponTargetProfile.cs
  - scripts/core/GameState.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-descriptor-combat-metadata.md
- Non-goals:
  - Do not delete `UnitDefinition` globally in this slice.
  - Do not delete `UnitDesignDefinitionCatalog.CompatibilityDefinition(...)` or
    `UnitKindDesignBridge.CompatibilityDefinition(...)` yet.
  - Do not change weapon balance, damage multipliers, target-profile data,
    movement, UI, art, fog, or live combat behavior.

Implementation summary:
- Removed the `UnitDefinition` unit-target overloads from
  `WeaponTargetProfile.CanTarget(...)` and `WeaponTargetProfile.Priority(...)`.
- Removed the `UnitDefinition` unit-target overloads from
  `GameState.EffectiveDamageAgainst(...)`, `WeaponCanTarget(...)`, and
  `WeaponTargetPriority(...)`.
- Moved CombatBehavior damage, target legality, and target-priority QA to pass
  `UnitSpecRuntimeDescriptor` values directly.
- Removed the aircraft target-profile QA's intermediate legacy-compatible
  `UnitDefinition` projection while keeping the separate compatibility bridge QA
  that still belongs to the remaining legacy-deletion milestone.
- Added `ReviewGate descriptorcombatmetadata` and updated historical
  UnitSpec read-path gates so they no longer force old `...Definition` variable
  names or aircraft combat metadata compatibility projections.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors after
    deleting the UnitDefinition combat metadata overloads.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors after
    adding the new descriptorcombatmetadata gate.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- descriptorcombatmetadata`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitdesigndefinitioncatalog`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after historical
    aircraft target-profile assertions were updated to descriptor input.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorunitspecreadpath`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after descriptor
    variable renaming.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateunitspeccombatmetadata`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings, preserving the older
    live GameState descriptor-read contract.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=descriptor-combat-metadata`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after evidence
    backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes only combat metadata API types and tool QA read
    paths; formulas and target-profile data are unchanged.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The read-only explorer confirmed that the real affected callers were
    CombatBehavior target-profile/damage QA and historical ReviewGate
    RequireText assertions.
- Residual risks:
  - `UnitDefinition` still exists as an explicit compatibility projection for
    remaining legacy UnitKind bridge checks.
  - Several historical ReviewGate gates still require compatibility projection
    APIs until the later whole-legacy deletion slice.

TODO update:
- Items marked done:
  - Descriptor-only combat metadata cleanup
- Items left open:
  - Broader UnitSpec duplicate-data cleanup and full `UnitDefinition`
    compatibility deletion remain open.
- Reason:
  - This slice removes only the combat metadata API's dependence on
    `UnitDefinition`; it does not remove every legacy compatibility projection.
