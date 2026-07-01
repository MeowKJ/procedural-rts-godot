# Review Record - M3 backend closure

Step:
Close already-implemented M3 backend authoring and power-gating TODO items.

Milestone:
M3 Build & Construction System.

Owner AI:
Previous construction backend slices, integrated by Codex main agent.

Reviewer AI:
Lovelace read-only M3 audit, ReviewGate, and deterministic replay.

Integrator AI:
Codex main agent.

Scope:
- Files/folders: `scripts/core/build/BuildSpec.cs`, `scripts/core/build/BuildSpecCatalog.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/core/sim/systems/construction/`, `scripts/core/sim/systems/ProductionSystem.cs`, `scripts/core/sim/systems/TurretCombatSystem.cs`, `tools/SimReplay/Core/ReplayPrelude.cs`, `TODO.md`, `docs/reviews/2026-07-01-m3-backend-closure.md`.
- Non-goals: no player HUD build buttons, no `BuildPlacementController` handoff, no ready-ticket placement consumption, no Dog/Cat UX implementation, and no restart/capture campaign UX.

Implementation summary:
- Verified `BuildSpec` is the single building/build authoring path after deletion of legacy building/build catalogs.
- Verified `BuildSpec.EntitySpecId` plus `BuildingTargetEntityBridge.ToEntitySpec()` provide the output entity spec path.
- Verified building seeds/components derive from `BuildSpec`: Construction, Power, Footprint, BuildRadius, ProductionQueue, Dock, Vision, WeaponUser, and PresentationPulse.
- Verified BuildRadius/power gating is implemented for placement through powered build anchors and for runtime consequences through production pause/speed and turret activity.
- Updated TODO to mark the backend-only `BuildSpec` and BuildRadius/power gating items complete while leaving player-facing construction UX and ready-ticket consumption open.

Automated gates:
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: construction loop, construction methods, queue-ready, power gate, visibility, cancel/refund, pause/offline, production, turret power, and `power-consequences` replays passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- m3backendclosure`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m3-backend-closure`
  Result: pass.
  Evidence: ReviewGate found this durable review record.

Manual/visual gates:
- Check: Not applicable.
  Result: not run.
  Evidence: this is a backend TODO status closure with no presentation changes.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: player build placement still uses the old immediate placement path, HUD build options are not wired, and Cat ready-to-place tickets cannot yet be consumed into placement.

TODO update:
- Items marked done: M3 `BuildSpec`; M3 BuildRadius / power gating.
- Items left open: faction construction UX, ConstructionSystem ready-ticket/placing/destroyed lifecycle, deterministic faction UX tests, and build/production UI handoff.
- Reason: backend evidence is complete, but player-facing construction flow remains separate work.
