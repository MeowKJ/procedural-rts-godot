# Review Record - Selection and footprint redraw throttling

Step:
Reduce battle-loop presentation redraw warnings for selection overlays and
footprint trails.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Mill subagent (`019f1009-10bc-7152-8c24-e1864d6a7c1c`).

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/controllers/SelectionController.cs`
  - `scripts/world/FootprintLayer.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-selection-footprint-redraw.md`
- Non-goals:
  - Do not change selection command semantics.
  - Do not change hover picking cadence.
  - Do not batch unit/building drawing in this slice.
  - Do not claim the full redraw TODO is complete.

Implementation summary:
- `SelectionController` still updates hover/preview state every frame, but redraw
  submission is throttled to 30Hz while idle and 60Hz while dragging.
- Drag start, drag motion, and drag clear still request immediate redraws for
  command responsiveness.
- `FootprintLayer` still updates mark age and emits marks every frame, but redraws
  at 30Hz.
- `ReviewGate presentation` dropped from 11 warnings to 9 warnings.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation`
  Result:
  Pass with warnings.
  Evidence:
  ReviewGate reported 0 errors and 9 remaining redraw warnings. Selection and
  Footprint warnings are no longer listed.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation --max-warnings=9`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 9 warnings, matching the new baseline.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings with this durable record present.

Manual/visual gates:
- Check:
  Independent Reviewer AI read-only audit.
  Result:
  Pass with warning.
  Evidence:
  Reviewer AI reported low risk for drag responsiveness because drag motion still
  queues immediate redraws and active drag redraws at 60Hz. Reviewer reported low
  risk for footprint fade because mark age is still updated every frame and 30Hz
  draw cadence is acceptable for the current trail lifetimes.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Add exact review-record verification to ReviewGate. Fixed with
    `--require-record=<name>`.
- Residual risks:
  - Visual QA in the Godot window has not been performed for drag feel.
  - Footprint fade may appear slightly less smooth at 30Hz, though state aging
    remains frame-rate independent.
  - HUD command preview still redraws from `HudLayer.SetCommandPreview()` through
    `BattleRoot.RefreshCommandPreview()` and is not covered by this slice.
  - Unit/building views still redraw every frame and remain open TODO work.

TODO update:
- Items marked done:
  - None; the broad redraw TODO remains open.
- Items left open:
  - Full view redraw cleanup.
  - Off-screen culling.
  - Unit/building batching or dirty snapshots.
- Reason:
  - Evidence proves a narrower redraw slice improved the automated baseline, but
    the full M6 presentation performance item is not complete.
