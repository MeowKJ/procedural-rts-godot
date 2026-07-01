# Review Record - Faction start UnitDesign loadout cleanup

Step:
- Faction start UnitDesign loadout cleanup

Milestone:
- M1 UnitSpec duplicate-data cleanup / Playable faction starts

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate factionstartbridge

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/FactionDefinition.cs
  - scripts/core/FactionCatalog.cs
  - scripts/core/MatchStartLoadout.cs
  - scripts/core/GameState.cs
  - scripts/core/units/UnitKindDesignBridge.cs
  - scripts/SkirmishFlowQaRoot.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-faction-start-unitspec-loadout.md
- Non-goals:
  - Do not delete `UnitKind` globally.
  - Do not change building start data or `BuildingKind`.
  - Do not change unit balance, art, UI layout, production rules, or faction
    roster contents.

Implementation summary:
- Removed duplicate legacy `StartingUnits` from `FactionDefinition` and
  `FactionCatalog`.
- `MatchStartLoadouts` now emits starting units by UnitDesign id from
  `UnitDesignRuntimeLoadouts`.
- Legacy `GameState` converts start design ids through
  `UnitKindDesignBridge.KindForDesignId(...)` only at the old runtime edge.
- CombatBehavior and SkirmishFlow QA now verify starts from UnitDesign runtime
  loadouts instead of reading `FactionCatalog.StartingUnits`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with UnitDesign starting-loadout assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- factionstartbridge`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- startloadout`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=faction-start-unitspec-loadout`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps, after the grouped availability cleanup batch.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes authoring/read paths only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `FactionCatalog.AvailableUnits` still exposes legacy `UnitKind` availability
    until a later whole-legacy deletion slice.
  - `BuildingKind` start data remains intentionally unchanged in this slice.

TODO update:
- Items marked done:
  - Faction start UnitDesign loadout cleanup
- Items left open:
  - Broader UnitSpec duplicate-data cleanup and final `UnitKind` /
    `BuildingKind` deletion remain open.
