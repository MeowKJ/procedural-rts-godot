# Review Record - UI factory menu/settings slice

Step:
Milestone: M7 - UI & Presentation Polish
Owner AI: Worker-M7
Reviewer AI: Pending
Integrator AI: Pending

Scope:
- Files/folders: `scripts/ui/UiFactory.cs`, `scripts/MainMenuRoot.cs`, `scripts/ui/SettingsOverlayLayer.cs`, `tools/ReviewGate/Program.cs`
- Non-goals: HUD, CommandPlate, minimap, battle overlays, art/unit/sim changes, `TODO.md` updates

Implementation summary:
- Added `UiFactory.StyleButton(BaseButton, Color)` so settings `CheckButton` and `OptionButton` controls share the same noncombat button theme as menu buttons.
- Replaced local MainMenu panel, label, button, and stylebox helpers with `UiFactory` calls while preserving menu layout, shortcuts, signals, scene launch behavior, and skirmish option state flow.
- Replaced local Settings panel, label, action button, and option-control styling helpers with `UiFactory` calls while preserving settings refresh/apply behavior.
- Added a narrow ReviewGate presentation check for MainMenu and Settings only, guarding against reintroduced local noncombat Label/Button/Panel style helpers.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate reported Errors: 0, Warnings: 0.

Manual/visual gates:
- Check: Static UI scope audit.
  Result: pass
  Evidence: `MainMenuRoot` and `SettingsOverlayLayer` now call `UiFactory` for repeated noncombat panels, labels, buttons, and settings option button styling; HUD, CommandPlate, minimap, and battle overlay files were not edited.

Reviewer result:
- Status: pending
- Required fixes: pending reviewer audit
- Residual risks: No runtime screenshot pass was performed, so this relies on code-level preservation plus build and ReviewGate evidence.

TODO update:
- Items marked done: none
- Items left open: broad M7 `UiTheme`/`UiFactory` extraction remains open for larger theme consolidation and any later HUD/battle-safe work.
- Reason: this is a bounded MainMenu/SkirmishSetup/Settings noncombat factory extraction only.
