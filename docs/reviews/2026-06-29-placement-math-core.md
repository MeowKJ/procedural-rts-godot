# Review Record - PlacementMath Core

Step: M3 buildable-area placement math slice
Milestone: Pure sim construction placement legality
Owner AI: Worker-M3
Reviewer AI: pending
Integrator AI: pending

Scope:
- Files/folders: `scripts/core/PlacementMath.cs`, `scripts/core/sim/systems/ConstructionSystem.cs`, `scripts/core/sim/SimEvent.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`.
- Non-goals: build UI/preview, faction construction UX, UnitSpec cleanup/UI/pathing, cancel/refund behavior, broad TODO updates.

Implementation summary:
- Added `PlacementMath.ValidateBuildableArea` for snapped footprint legality with build-radius anchors, passable terrain/domain sampling, world bounds, and footprint obstacle overlap.
- Routed `ConstructionSystem` placement legality through `PlacementMath` and emitted `ConstructionRejectedEvent` with stable failure reason keys.
- Extended SimReplay construction coverage for legal construction plus missing tech, overlap, outside build radius, and impassable terrain rejection.
- Added a narrow `ReviewGate placementmath` mode for this slice.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed; construction-loop reported 3 buildings, 80 credits, 5 rejected placements, and deterministic hash `5D1A493543651765`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj placementmath --no-restore`
  Result: pass
  Evidence: ReviewGate placementmath passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual/UI placement preview
  Result: not run
  Evidence: Not in scope for this pure sim slice.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: pending reviewer.
- Residual risks: Tech/power/fog construction constraints remain broader follow-up work; this slice covers the minimal PlacementMath legality path.

TODO update:
- Items marked done: none.
- Items left open: broad buildable-area system TODO remains open.
- Reason: User explicitly requested not to update `TODO.md`; this is a narrow acceptance slice, not the whole buildable-area feature.
