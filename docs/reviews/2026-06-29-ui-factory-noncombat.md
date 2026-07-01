# Review Record - UI factory noncombat slice

Step:
Milestone: M7 - UI & Presentation Polish
Owner AI: Worker B
Reviewer AI: Pending
Integrator AI: Pending

Scope:
- Files/folders: `scripts/ui/UiFactory.cs`, `scripts/ui/PauseMenuLayer.cs`, `scripts/ui/OutcomeScreenLayer.cs`, `TODO.md`
- Non-goals: HUD/build/production UI, combat UI behavior, screenshot automation, scene flow changes

Implementation summary:
- Added a narrow noncombat `UiFactory` for shared panel, label, button, and button style construction.
- Replaced duplicated Pause/Outcome panel, label, button, and button-style helpers with `UiFactory` calls.
- Preserved existing colors, sizing, text, callbacks, panel radius, button margins, and per-screen label outline alpha.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation --max-warnings=0 --no-restore`
  Result: pass
  Evidence: ReviewGate reported Errors: 0, Warnings: 0.

Manual/visual gates:
- Check: Static comparison of extracted helper values against the previous Pause/Outcome helpers.
  Result: pass
  Evidence: Panel fill/stroke inputs remain call-site specific; shared style keeps 3px corners and 1px border; button colors/margins/font size match the removed duplicated code; Outcome labels still pass outline alpha `0.80f` while Pause uses the factory default `0.78f`.

Reviewer result:
- Status: pending
- Required fixes: pending reviewer audit
- Residual risks: No runtime screenshot pass was performed, so this relies on code-level preservation plus build/presentation gates.

TODO update:
- Items marked done: none
- Items left open: broad M7 `UiTheme`/`UiFactory` extraction remains open for MainMenu/Settings/Hud and any larger theme consolidation.
- Reason: this is a bounded noncombat overlay slice only.
