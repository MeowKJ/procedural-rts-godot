# Review Record - UnitKind cleanup edges

Step: M1 UnitKind cleanup edges
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex plus worker agents
Reviewer AI: ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/core/game-state/GameState.SeedingMap.cs`, `tools/FogOfWarQa/Program.cs`, `tools/CombatBehavior/Program.cs`, `tools/CombatBehavior/Scenarios/Catalogs.cs`, `tools/CombatBehavior/Scenarios/SkirmishAi.cs`, `tools/ReviewGate/ContentAuthoringReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-unitkind-cleanup-edges.md`.
- Non-goals: deleting `UnitKind`, deleting `BuildingKind`, changing combat/movement behavior, or changing balance.

Implementation summary:
- Removed the `AddUnit(UnitKind...)` seeding wrapper and changed developer sandbox generic units to `generic.light_tank`, `generic.infantry`, and `generic.harvester` design ids.
- Changed developer sandbox faction rows to spawn every faction roster unit directly from `UnitDesignFactionRosterCatalog.PlayableDesignIds`, including data-only units that have no legacy `UnitKind`.
- Changed `FogOfWarQa` unit fixtures from `UnitKind` to design-id fixtures backed by `UnitDesignCatalog.Spec(...)` and `UnitDesignDefinitionCatalog.RuntimeDescriptors`; fixtures no longer populate `LegacyKind`.
- Changed CombatBehavior skirmish start and sandbox roster checks to assert `UnitModel.DesignId` instead of converting starting/playable design ids back to `UnitKind`.
- Removed `GameState.UnitRuntimeDescriptorFor(UnitKind)` and the public `GameState.IsHarvesterUnit(UnitKind)` helper; the core runtime now keeps the UnitModel/UnitSpec harvester path only.
- Added ReviewGate coverage so these cleanup edges cannot regress.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet build tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed all behavior checks.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: Fog-of-war QA completed with design-id unit fixtures.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestatesandboxrosterunitspec`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- fogofwarqaunitspecreadpath`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateharvesterunitspec`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitmodeldesignidnative`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, FogOfWarQa, ReviewGate, PerfSmoke, BalanceReport, and Godot headless QA.

Manual/visual gates:
- Check: not applicable
  Result: pass
  Evidence: no visual output changed.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `UnitKind` still remains for explicit legacy combat/movement fixtures, presentation compatibility checks, and the `UnitModel.Kind` compatibility projection. `BuildingKind` remains the BuildSpec/construction identity and is not ready for deletion.

TODO update:
- Items marked done: none; this is progress under UnitSpec phase-3 duplicate-data cleanup.
- Items left open: final legacy `UnitKind`/`BuildingKind` deletion and remaining compatibility edges.
- Reason: the live/sandbox/Fog QA/skirmish paths moved farther toward native UnitDesign ids, but legacy fixtures and bridge checks still intentionally exist.
