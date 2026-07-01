# Review Record - BuildingDefinitions deletion cleanup

Step: Migration cleanup BuildingDefinitions deletion slice
Milestone: M1 EntityWorld Becomes Authoritative / M3 Build & Construction System
Owner AI: Codex
Reviewer AI: Banach explorer, Nash explorer, ReviewGate buildingdefinitionsdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/BuildSpec.cs`, `scripts/core/BuildingDefinition.cs`, `scripts/core/BuildDefinition.cs`, `scripts/core/BuildCatalog.cs`, `scripts/core/GameState.cs`, `scripts/core/WeaponTargetProfile.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-building-definitions-deleted.md`.
- Non-goals: deleting `BuildingKind`, deleting `UnitBattlefieldBuildingTarget`, rewriting construction methods, changing building balance, or changing BuildSpecCatalog authored values.

Implementation summary:
- Deleted the old `BuildingDefinition`, `BuildDefinition`, and `BuildCatalog` compatibility shells.
- Removed `BuildSpec` legacy projection/merge helpers for those deleted records.
- Removed `GameState.BuildingDefinitions` plus building-target combat overloads that accepted `BuildingDefinition`.
- Removed `WeaponTargetProfile` overloads for `BuildingDefinition`; building legality and priority now use `BuildSpec`.
- Updated ReviewGate so structure, turret, resource, BuildSpec authority, and deleted-symbol checks protect `BuildSpecCatalog` as the single building/build authoring catalog.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `rg -n "BuildingDefinition|BuildDefinition|BuildCatalog|BuildingDefinitions|ToBuildingDefinition|ToBuildDefinition|BuildSpec\.From\(" scripts -S`
  Result: pass
  Evidence: no script references remained; `rg` exited with no matches.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingdefinitionsdeleted`
  Result: pass
  Evidence: ReviewGate buildingdefinitionsdeleted completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildspecbridge`
  Result: pass
  Evidence: ReviewGate buildspecbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildspecauthority`
  Result: pass
  Evidence: ReviewGate buildspecauthority completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- resourcescope`
  Result: pass
  Evidence: ReviewGate resourcescope completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- structures`
  Result: pass
  Evidence: ReviewGate structures completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- turrets`
  Result: pass
  Evidence: ReviewGate turrets completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: SimulationSmoke passed 300 seconds with 10 orders, 10 completions, 2 waves, and outcome InProgress.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=building-definitions-deleted`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks, including build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa, ReviewGate, PerfSmoke, Godot battle headless, active battle perf, skirmish flow, and pause QA.

Manual/visual gates:
- Check: visual inspection not required for this deleted compatibility-layer cleanup.
  Result: not run.
  Evidence: no presentation behavior or authored values changed.

Reviewer result:
- Status: pass
- Required fixes: none before automated verification; explorers independently found the deleted layers were compatibility-only and named the ReviewGate updates required.
- Residual risks: `UnitBattlefieldBuildingTarget` remains a second building runtime migration shell; this slice only removes old static definition/catalog compatibility layers.

TODO update:
- Items marked done: `BuildingDefinitions deletion cleanup` subitem under Migration cleanup.
- Items left open: parent Migration cleanup and M3 `BuildSpec` one-authoring-path item remain open.
- Reason: deleted definition/catalog shells are gone, but the broader building runtime and construction/economy entity integration are not fully retired.
