# Review Record - M3 player construction handoff

Step:
Route player build placement through the live ConstructionSystem path.

Milestone:
M3 Build & Construction System.

Owner AI:
Codex main agent.

Reviewer AI:
PlayerLoopQa, ReviewGate, and Lovelace M3 read-only audit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders: `scripts/controllers/BuildPlacementController.cs`, `scripts/BattleRoot.cs`, `scripts/BattleRoot.Lifecycle.cs`, `scripts/BattleRoot.Process.cs`, `scripts/core/game-state/GameState.SeedingMap.cs`, `tools/PlayerLoopQa/Program.cs`, `TODO.md`, `docs/reviews/2026-07-01-m3-player-construction-handoff.md`.
- Non-goals: no Cat ready-ticket placement consumption, no Dog deploy-unit UX, no restart/capture UX, no HUD build tab redesign, and no balance changes.

Implementation summary:
- `BuildPlacementController` now previews and confirms placement through `UnitBattlefield` legality/construct APIs instead of legacy instant `GameState.PlaceBuildingWithinBuildRadius`.
- Accepted player placement calls `UnitBattlefield.ConstructBuilding`, spending credits immediately and creating an under-construction entity driven by `ConstructionSystem`.
- `BattleRoot` now adopts runtime building snapshots into lightweight `GameState` view models when live construction creates a new building, so existing `BuildingView` rendering can display the construction projection.
- `GameState.UpsertRuntimeBuilding` keeps the legacy view model synchronized from the runtime snapshot/projection without making the legacy model authoritative.
- `PlayerLoopQa` now proves player construction handoff spends credits, starts below full build progress, and completes through the shared construction system.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass.
  Evidence: PlayerLoopQa passed after replacing old instant-placement coverage with live construction handoff assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- m3playerconstructionhandoff`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m3-player-construction-handoff`
  Result: pass.
  Evidence: ReviewGate found this durable review record.

Manual/visual gates:
- Check: Godot headless battle boot through VerifyAll.
  Result: pass.
  Evidence: full `VerifyAll` includes `godot-battle-headless` and related scene QA after this handoff.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: the build UI is still hotkey/preview based; faction-specific construction UX, ready-to-place ticket consumption, and restart/capture flows remain separate open TODO work.

TODO update:
- Items marked done: none; this is a meaningful progress slice inside still-open M3/UI parent items.
- Items left open: faction construction UX, ready-ticket placement, ConstructionSystem destroyed/placing lifecycle, build tabs, and refund feedback.
- Reason: player placement now reaches the construction backend, but the broader per-faction construction UX is intentionally not closed by this slice.
