# Review Record - Overlay and effect culling

Step:
Finish off-screen culling coverage for battle overlays and effect drawing.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Carson subagent (`019f101e-b18f-7002-b9f7-a5f9adc62294`).

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/BattleRoot.cs`
  - `scripts/world/CombatEffectsLayer.cs`
  - `scripts/world/CommandAcknowledgementLayer.cs`
  - `scripts/world/FootprintLayer.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-overlay-effect-culling.md`
- Non-goals:
  - Do not change gameplay simulation or effect lifetimes.
  - Do not pool VFX objects in this slice.
  - Do not implement MultiMesh/atlas batching.
  - Do not visually QA every effect in the Godot window.

Implementation summary:
- `BattleRoot.RefreshViewCulling()` now passes the padded camera rect to combat
  effects, command acknowledgements, and footprints.
- `CombatEffectsLayer` still ages/removes effects every frame, but skips off-screen
  drawing for unit deaths, threat alerts, beams, projectiles, and hit pulses.
- `CommandAcknowledgementLayer` still ages rings every frame, but skips off-screen
  ring drawing.
- `FootprintLayer` still ages and emits marks every frame, but skips off-screen
  mark drawing.
- `ReviewGate culling` now checks both view culling and overlay/effect culling.
- The TODO item "Off-screen culling..." is now marked done; pooling and batching
  remain separate open TODO work.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj culling`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=overlay-effect-culling`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings with this durable record present.

Manual/visual gates:
- Check:
  Independent Reviewer AI read-only audit.
  Result:
  Pass with warnings.
  Evidence:
  Reviewer AI confirmed state continues to age/removal in `_Process`, culling only
  affects `_Draw`, beams use segment bounding boxes, and command/footprint state is
  not deleted by culling. Reviewer noted the lack of Godot-window visual QA and the
  static nature of `ReviewGate culling`.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Replace pending reviewer fields with the actual reviewer result. Fixed.
- Residual risks:
  - `ReviewGate culling` is static and does not prove visual continuity at runtime.
  - `FootprintLayer.IsVisible` culls by mark center rather than mark bounds; current
    culling margin makes edge loss unlikely.
  - VFX pooling/capping remains open as a separate performance TODO.
  - Visual QA in the Godot window has not been performed.

TODO update:
- Items marked done:
  - `Off-screen culling via CameraController.VisibleWorldRect() (+margin) /
    VisibleOnScreenNotifier2D; skip _Draw and hide off-screen views.`
- Items left open:
  - Batch unit bodies / atlas.
  - Pool combat VFX/footprints and cap concurrent effects.
  - GridLayer cached texture/MultiMesh or visible rect.
- Reason:
  - View culling and overlay/effect draw culling now share the same camera rect, and
    `ReviewGate culling` verifies coverage.
