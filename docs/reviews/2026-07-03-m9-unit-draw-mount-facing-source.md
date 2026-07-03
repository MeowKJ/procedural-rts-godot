# Review Record - M9 Unit Draw Mount Facing Source

Step: #196 `[M9] Replace unit draw mount-facing dictionaries`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / DesktopHudQa
Integrator AI: Remote Linux Codex

Scope:
- Added `UnitMountFacingSource` so unit art rendering can resolve runtime, legacy, and single-icon mount facings without dictionary construction.
- Routed `UnitInstanceView` through existing runtime `WeaponMounts` storage instead of `Unit.MountFacings()`.
- Routed `DynamicUnitIcon` through `UnitMountFacingSource.Single("main", turretFacing)` instead of a per-draw dictionary literal.
- Routed legacy `UnitView` through `UnitMountFacingSource.FromLegacyUnit(...)` and removed its mutable dictionary cache.
- Added `UnitRenderingAllocationReviewGate` under `ReviewGate presentation` to lock the no-dictionary draw contract.
- Non-goals: changing art recipes, layer ordering, turret/body facing semantics, combat mount state, batching, or broad UI polish.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- This is a source-structure allocation guard; it does not include a direct allocation profiler sample.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #196 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
