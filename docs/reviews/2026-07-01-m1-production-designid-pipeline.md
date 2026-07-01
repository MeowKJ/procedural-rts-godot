# Review Record - M1 production DesignId and shared pipeline

Step: M1 production duplicate-data cleanup and live pipeline wiring
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate / delegated read-only explorers
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/SimSystemPipeline.cs`, `scripts/BattleRoot.EntityWorld.cs`, `tools/SimReplay/Core/SimSystemPipelineScenarios.cs`, `tools/SimReplay/Core/ReplayPrelude.cs`, `scripts/core/production/`, `scripts/core/game-state/GameState.EconomyBuild.cs`, `scripts/core/game-state/GameState.ProductionHarvest.cs`, `scripts/core/units/ProductionKindDesignBridge.cs`, `scripts/core/ai/EnemyProductionAi.cs`, `tools/SimulationSmoke/Program.cs`, `tools/ReviewGate/checks/unit-spec/`, `tools/ReviewGate/checks/commands-combat/`, `TODO.md`.
- Non-goals: final deletion of `UnitKind`/`BuildingKind`, combat-system convergence, or changing balance values.

Implementation summary:
- Added `SimSystemPipeline.ConfigureLiveGameplay(...)` as the canonical live EntityWorld system order and made `BattleRoot.ConfigureEntityWorld()` call it.
- Added `AssertLiveSimSystemPipeline()` to SimReplay so the shared live pipeline is executable under the determinism harness.
- Updated ReviewGate pipeline checks to read `BattleRoot` plus `SimSystemPipeline` evidence instead of requiring hand-written `AddSystem` calls in `BattleRoot`.
- Added `DesignId` to legacy `ProductionQueueItem`, `ProductionQueueSnapshot`, and `CompletedProductionItem`.
- Migrated GameState production runtime, lane snapshots, completion labels, HUD fallback, BuildingView progress bars, EnemyProductionAi, and SimulationSmoke to concrete UnitDesign reads.
- Deleted `ProductionKindDesignBridge.LegacySpecFor`, `LegacyProductionSpecs`, and `BuildLegacyProductionSpecs`.
- Updated ReviewGate so those deleted ProductionKind-only legacy spec tables are now forbidden.
- Updated CombatBehavior production/economy assertions to verify concrete Dog UnitDesign outputs instead of the old generic `UnitKind.Infantry`/three-button assumptions.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateproductionruntimeunitspec`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- productionkinddesignbridge`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- entitypathfinding`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitspeccleanup`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateproductionoptionsunitspec`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateproductionlanesunitspec`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- externalproductiondefinitionscleanup`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- productiondefinitionsdeleted`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simulationsmokeproductionspecreadpath`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- enemyproductionaiunitspecproduction`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- resourcescope`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: local run completed after upgrading production assertions to concrete UnitDesign ids.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, SimulationSmoke, ReviewGate, perf smoke, and Godot headless QA.

Reviewer result:
- Status: pass
- Required fixes: none from narrow gates.
- Residual risks: combat convergence remains intentionally separate because mixing the three combat systems in one pipeline risks double damage resolution.

TODO update:
- Items marked done: shared live `SimSystemPipeline` wiring gap.
- Items left open: final UnitSpec phase-3 duplicate-data cleanup and final legacy `UnitKind`/`BuildingKind`/`UnitCatalog` deletion.
- Reason: production duplicate data is materially reduced, but presentation/compatibility identity edges still remain and should be deleted only after their consumers move to concrete UnitDesign/BuildSpec ids.
