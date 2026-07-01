# Review Record - HUD UI factory style slice

Step:
Move repeated HUD style construction into the shared UiFactory without changing HUD
layout or gameplay behavior.

Milestone:
M7 - UI and art.

Owner AI:
Integrator / Codex main thread.

Reviewer AI:
Integrator self-check plus automated presentation gate.

Integrator AI:
Codex main thread.

Scope:
- Files/folders: `scripts/ui/UiFactory.cs`, `scripts/ui/HudLayer.cs`,
  `TODO.md`, `docs/reviews/2026-06-29-hud-ui-factory-style.md`.
- Non-goals: no HUD layout redesign, no CommandPlate behavior changes, no build or
  production UI feature work, no art/unit/sim changes, no screenshot automation.

Implementation summary:
- Added HUD-specific `UiFactory.HudPanelStyle(...)`,
  `UiFactory.MakeHudLabel(...)`, `UiFactory.ApplyHudLabelShadow(...)`, and
  `UiFactory.ApplyHudButtonTheme(...)`.
- Repointed `HudLayer` panel, label, shadow, and button-state styling helpers to
  the shared factory while preserving existing positions, sizes, callbacks, and
  visual-theme refresh flow.
- Kept layout convenience helpers in `HudLayer`, because the broader HUD
  composition extraction is still open.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation --max-warnings=0 --no-restore`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Static scope audit.
  Result: pass.
  Evidence: only HUD style construction plumbing changed; HUD layout constants,
  positions, signal hookups, command buttons, minimap, and sim-facing behavior were
  not intentionally changed.

Reviewer result:
- Status: pass for this bounded style-factory slice.
- Required fixes: none.
- Residual risks: no screenshot pass was captured for this small refactor; final
  visual confirmation remains useful when the broader HUD/build UI slice starts.

TODO update:
- Items marked done: none; the broad `UiTheme`/`UiFactory` extraction remains open.
- Items left open: full HUD composition extraction, build/production UI, upgrade UI,
  and any later screenshot-based HUD QA.
- Reason: this slice centralizes repeated HUD styling but deliberately avoids
  claiming the larger M7 UI milestone is complete.
