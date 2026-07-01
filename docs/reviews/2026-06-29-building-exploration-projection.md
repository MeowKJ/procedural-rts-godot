Step: Route BuildingView explored-memory draw filtering through EntityWorld building presentation projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/world/BuildingView.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated `BuildingView._Draw` so live building visibility checks compute world rects from `BuildingPresentationProjection` position and footprint.
- Friendly/self building draw visibility now derives from projected `OwnerId`; enemy building draw visibility uses fog explored-memory checks against the projected rect.
- Kept `State.IsExploredByPlayer(Building)` as the old-runtime fallback.
- Non-goals: no FogOfWarMap rewrite, no BuildingView art identity rewrite, no removal of `BuildingModel`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingexplorationprojection --no-restore`
  Result: pass
  Evidence: building exploration projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: this keeps fog authority in `GameState.FogOfWar`, while using EntityWorld projection geometry for the building being drawn.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingView` still uses legacy building kind/owner/faction for art identity.
- Exploration checks still consume `GameState.FogOfWar` until vision/fog is fully projected.
- Full `BuildingModel` removal remains open.

TODO update:
- Marked done: nested M1 slice `BuildingView exploration projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
