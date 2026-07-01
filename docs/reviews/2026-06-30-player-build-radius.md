# Review Record - Player build radius

Step: Player live build placement uses owner build radius.
Milestone: Playable 1v1 skirmish vertical slice.
Owner AI: Main Codex thread.
Reviewer AI: Integrator sanity review.
Integrator AI: Main Codex thread.

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/controllers/BuildPlacementController.cs`, `scripts/core/GameText.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-30-player-build-radius.md`.
- Non-goals: No Cat sidebar UI handoff, no construction queue cost/timer integration for the legacy live placement controller, no deletion of raw test fixture `PlaceBuilding`, no full player-loop TODO closure.

Implementation summary:
- Added owner-aware `GameState.ValidateBuildingPlacement(BuildingKind, Owner, Vector2)` using `PlacementMath.ValidateBuildableArea`.
- Added `GameState.PlaceBuildingWithinBuildRadius(...)` as the player-safe placement entrypoint while preserving raw `PlaceBuilding(...)` for deterministic fixtures.
- `BuildPlacementController` preview and click placement now use the owner-aware player path.
- Build-radius anchors derive from completed, alive, powered owner buildings with `BuildSpec.BuildRadius`.
- Added localized placement failure labels for outside build radius, unpowered authority, impassable terrain, and not-visible reasons.
- CombatBehavior now proves inside-radius player placement succeeds, outside-radius placement rejects with `placement.outsideBuildRadius`, and the safe entrypoint places a factory used by production prerequisite tests.

Automated gates:
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: Pass.
  Evidence: `Combat behavior passed: weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- playerbuildradius`
  Result: Pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- Check: None.
  Result: Not applicable for this backend/controller contract slice.

Reviewer result:
- Status: pass with residual risk.
- Required fixes: None for this slice after gate pass.
- Residual risks: Live legacy placement still places instantly and does not yet consume credits or run the full Cat sidebar/Dog deploy construction UX; those remain tracked under M3 and UI construction TODOs.

TODO update:
- Items marked done: None at top-level.
- Items left open: Full `Player can...` vertical slice item remains open.
- Reason: This closes the build-radius portion of the player loop, but does not by itself prove every player action in that broad acceptance item.
