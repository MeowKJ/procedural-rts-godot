# Review Record - Menu and showcase redraw throttling

Step:
Finish the unconditional per-frame redraw cleanup by throttling menu and showcase
root drawing.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Huygens subagent (`019f1012-8f66-70e3-9c6e-e16f432f57f1`).

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/MainMenuRoot.cs`
  - `scripts/OverallStyleShowcaseRoot.cs`
  - `scripts/StyleCandidateDeckRoot.cs`
  - `scripts/StyleFamilyShowcaseRoot.cs`
  - `scripts/StyleTestRoot.cs`
  - `scripts/UnitShowcaseRoot.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-menu-showcase-redraw.md`
- Non-goals:
  - Do not change battle gameplay.
  - Do not change generated art/layout.
  - Do not complete off-screen culling, grid caching, or batching.
  - Do not remove showcase animations.

Implementation summary:
- `MainMenuRoot` backdrop redraw is throttled to 20Hz while `_elapsed` still updates
  every frame.
- Style and unit showcase roots redraw at 20Hz instead of every frame.
- `StyleCandidateDeckRoot` keeps immediate redraw on left/right/number selection
  changes.
- `StyleCandidateDeckRoot` also supports numpad `Kp1`-`Kp6` selection after reviewer
  feedback.
- `ReviewGate presentation` dropped from 6 warnings to 0 warnings.
- The TODO item "Stop unconditional per-frame QueueRedraw in every view" is now
  marked done; broader culling/cache/batching items remain open.

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
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation --max-warnings=0`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings at the new zero-warning baseline.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=menu-showcase-redraw`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings with this durable record present.

Manual/visual gates:
- Check:
  Independent Reviewer AI read-only audit.
  Result:
  Pass with residual risks.
  Evidence:
  Reviewer AI found no blocking issues. Menu elapsed time still advances every
  frame, capture scenes wait multiple frames before saving, and StyleCandidate
  left/right/number changes now redraw immediately.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Support numpad `Kp1`-`Kp6` in `StyleCandidateDeckRoot`. Fixed.
- Residual risks:
  - Visual QA in the Godot window has not been performed.
  - Showcase animations are now 20Hz, which may be less smooth but should not affect
    gameplay.
  - Capture scenes rely on initial/periodic redraw; automated screenshot capture has
    not been re-run in this slice.
  - `ReviewGate presentation` proves there are no unthrottled `_Process ->
    QueueRedraw()` patterns; it does not prove visual smoothness by itself.

TODO update:
- Items marked done:
  - `Stop unconditional per-frame QueueRedraw() in every view; redraw on dirty/throttle.`
- Items left open:
  - Off-screen culling.
  - `GridLayer` cached texture/MultiMesh or visible rect.
  - Unit batching / pooled VFX.
- Reason:
  - `ReviewGate presentation` now has a zero-warning baseline for unconditional
    `_Process` redraw patterns, proving this narrow TODO item is complete.
