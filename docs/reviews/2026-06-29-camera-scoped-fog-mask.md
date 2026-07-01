# Review Record - Camera scoped fog mask

Step: Scope world fog mask updates to the camera culling rect while preserving off-screen explored memory.
Milestone: Fog of war rendering.
Owner AI: Codex.
Reviewer AI: Codex self-review with FogOfWarQa and ReviewGate coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/FogOfWarMap.cs`, `scripts/world/FogOfWarLayer.cs`, `scripts/BattleRoot.cs`, `tools/FogOfWarQa/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-camera-scoped-fog-mask.md`.
- Non-goals: no change to gameplay visibility authority, no fog-of-war shader redesign, no minimap behavior change, no entity vision-system migration.

Implementation summary:
- Added an optional `Rect2? updateWorldRect` parameter to `FogOfWarMap.MaskTexture` so callers can update only a mask-cell range.
- Initial texture creation and minimap/all-map calls still refresh the full mask.
- Partial updates keep `_maskTextureDirty` true unless the update covers the full map, which preserves off-screen dirty explored-memory changes until a full update occurs.
- `FogOfWarLayer` now accepts `VisibleWorldRect` and requests a scoped mask update for the main world fog draw.
- `BattleRoot.RefreshViewCulling()` feeds the same camera rect plus margin used by other presentation layers into fog rendering.
- `FogOfWarQa` and `ReviewGate fog` now assert the camera-scoped fog path and the off-screen dirty-memory safeguard.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj fog --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for fog coverage.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: FogOfWarQa passed including camera-scoped texture update checks.

Manual/visual gates:
- Check: rendered fog visual comparison
  Result: not run
  Evidence: this slice changes the mask update scope, not visual policy; Godot headless smoke is covered by VerifyAll.

Reviewer result:
- Status: pass
- Required fixes: none after the scoped range and partial-dirty safeguards were added.
- Residual risks: texture upload still updates the existing Godot texture object after scoped CPU-side mask writes; deeper GPU upload-region optimization would need engine-specific support or a tiled mask design.

TODO update:
- Items marked done: `Scope fog recompute to camera rect (+margin) for maps larger than the screen; keep off-screen explored memory cached`.
- Items left open: broader fog quality/readability and entity-vision migration tasks.
- Reason: world fog rendering now requests camera-scoped mask-cell updates, and static QA proves partial updates do not clear off-screen dirty memory before a full update.
