# Review Record - Worker C Soft Old City Visual Slice

Step:
Improve Dog/Cat unit and building line-art language in the selected Soft Old City
direction without bitmap or SVG assets.

Milestone:
Playable 1v1 skirmish vertical slice - Soft Old City readability.

Owner AI:
Worker C.

Reviewer AI:
Integrator visual review plus automated gates.

Integrator AI:
Main Codex thread.

Scope:
- Files/folders:
  - `scripts/core/units/dog/DogUnitArt.cs`
  - `scripts/core/units/cat/CatUnitArt.cs`
  - `scripts/world/BuildingView.cs`
  - `docs/reviews/2026-06-30-worker-c-soft-old-city-visual.md`
- Non-goals:
  - No TODO checkbox update in this worker slice.
  - No VerifyAll or ReviewGate wiring changes.
  - No bitmap, SVG, or external art asset introduction.
  - No combat, production, or roster-data change.

Implementation summary:
- Dog unit recipes now use heavier hulls, armor plate lines, tread edges,
  repair/shield/assault mount variants, and small owner-color decals.
- Cat unit recipes now use slimmer swept hulls, crescent-like linework,
  blade/crescent mount variants, and small owner-color decals.
- Building owner color was reduced from body-spanning stripes to shorter decal
  plaques so ownership does not overwrite the body art.
- Airfield and turret buildings received distinct Soft Old City line motifs;
  turrets render a fixed platform plus a separately rotated mount.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build passed with 0 warnings and 0 errors.
- Command:
  `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\VisualQaCapture.ps1`
  Result:
  Pass.
  Evidence:
  Visual QA regenerated battle HUD screenshots for day, fog, dusk, and desktop
  viewport sizes under `artifacts/visual-qa/`.

Manual/visual gates:
- Check:
  Integrator reviewed `battle_hud_1920x1080.png`, `battle_hud_style1b_fog.png`,
  and `battle_hud_style1c_dusk.png`.
  Result:
  Pass with follow-up polish.
  Evidence:
  Unit bodies now show clearer heavy Dog silhouettes and small owner-color decals;
  fog and dusk remain readable. Buildings still have somewhat busy frame linework
  and should be refined in a later visual pass.

Reviewer result:
- Status: pass
- Required fixes:
  - Review record was expanded to the required protocol fields.
  - Removed per-layer `EnvironmentResponse.OwnerProtected` overrides from Dog/Cat
    owner decal layers; owner color protection now stays centralized in
    `EntityRenderPalette`.
- Residual risks:
  - The screenshots are not a same-screen Dog/Cat sandbox lineup, so faction
    contrast should still get a dedicated sandbox capture.
  - Building line density may still be too high for the final Soft Old City target.

TODO update:
- Items marked done:
  - None.
- Items left open:
  - The broad Soft Old City readability TODO remains open until owner color,
    HUD obstruction, and full faction silhouette checks all pass together.

Gate tag:
soft-old-city-visual
