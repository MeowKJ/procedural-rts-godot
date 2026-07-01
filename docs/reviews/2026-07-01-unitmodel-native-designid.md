# Review Record - UnitModel native DesignId identity

Step: M1 UnitModel native DesignId identity cleanup
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/UnitModel.cs`, `scripts/core/game-state/GameState.SeedingMap.cs`, `scripts/core/game-state/GameState.ProductionHarvest.cs`, `scripts/core/game-state/GameState.Selection.cs`, `scripts/core/game-state/GameState.RemovalDamageUtilities.cs`, `scripts/core/production/CompletedProductionItem.cs`, `scripts/core/production/ProductionQueueSnapshot.cs`, `scripts/core/presentation/descriptors/ProductionPresentationDescriptor.cs`, `scripts/core/presentation/descriptors/UnitPresentationCatalog.cs`, `scripts/core/presentation/descriptors/EntityPresentationDescriptor.cs`, `scripts/core/presentation/descriptors/PresentationCatalog.cs`, `scripts/core/units/UnitDeathInfo.cs`, `scripts/ui/DynamicUnitIcon.cs`, `scripts/ui/hud/HudLayer.State.cs`, `scripts/ui/hud/HudLayer.CommandControls.cs`, `scripts/ui/hud/HudLayer.NestedControls.cs`, `scripts/BattleRoot.HudSync.cs`, `scripts/BattleRoot.Selection.cs`, `tools/CombatBehavior/Program.cs`, `tools/FogOfWarQa/Program.cs`, `tools/ReviewGate/checks/`, `tools/ReviewGate/registry/ReviewGateUnitSpecEntries.cs`, `TODO.md`.
- Non-goals: deleting `UnitKind`, deleting `BuildingKind`, changing UnitBattlefield runtime identity, or changing balance.

Implementation summary:
- Changed legacy `UnitModel` so `DesignId` is required native identity and `LegacyKind` is optional compatibility data.
- Changed `UnitModel.Kind` into a compatibility projection from `LegacyKind` or `UnitKindDesignBridge.KindForDesignId(DesignId)`.
- Updated GameState start loadout seeding to create units directly from `MatchStartUnit.DesignId` instead of converting back to `UnitKind`.
- Updated old GameState production completion to compute spawn geometry and spawn units from queued/completed UnitDesign ids.
- Removed legacy `OutputUnit` from `CompletedProductionItem`.
- Removed legacy `Kind` from `UnitDeathInfo`.
- Changed same-type selection to group by `DesignId` instead of `UnitKind`.
- Removed legacy `OutputUnit` from `ProductionQueueSnapshot` and `ProductionPresentationDescriptor`.
- Changed generic production presentation fallback descriptors to store explicit UnitDesign ids.
- Removed HUD/icon `UnitKind?` fallback fields from `DynamicUnitIcon`, command buttons, portraits, and selection icon summaries.
- Removed `UnitKind` identity from `EntityPresentationDescriptor`.
- Updated legacy CombatBehavior and FogOfWarQa fixtures to seed `UnitModel.DesignId` explicitly while keeping `LegacyKind` as fixture compatibility.
- Added `ReviewGate unitmodeldesignidnative` to prevent `UnitModel.DesignId` from regressing into a computed `UnitKind` bridge property.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitmodeldesignidnative`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateproductionruntimeunitspec`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- startloadout`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestatesandboxrosterunitspec`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- externalproductiondefinitionscleanup`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitpresentationunitsdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- dynamicuniticonunitspec`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentationcatalogunitunitspec`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: local run completed with all behavior checks passing.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: local run completed with all smoke checks passing.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: local run completed with fog QA passing.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, SimulationSmoke, ReviewGate, perf smoke, and Godot headless QA.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `UnitKind` still exists as the old fixture/UI/compatibility enum and `BuildingKind` remains authoritative for BuildSpec/construction identity.

TODO update:
- Items marked done: none; this is a progress slice under UnitSpec phase-3 duplicate-data cleanup.
- Items left open: final deletion of `UnitKind`, `BuildingKind`, and remaining UnitKind conversion edges.
- Reason: UnitModel identity is now DesignId-native, but legacy UI, fixture, sandbox, and compatibility bridge surfaces remain.
