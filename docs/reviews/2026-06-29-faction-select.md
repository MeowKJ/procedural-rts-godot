# Review Record - Faction select

Step: Add player and AI faction selection to the skirmish setup path.
Milestone: Playable 1v1 Skirmish - setup flow.
Owner AI: Codex.
Reviewer AI: Codex self-review (bounded slice; ReviewGate and behavior tests provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/MainMenuRoot.cs`, `scripts/core/GameText.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-faction-select.md`.
- Non-goals: no full boot-to-battle runtime UI automation, no third faction placeholder UI, no campaign setup, no visual redesign of the menu.

Implementation summary:
- Added player faction and AI faction dropdowns to the main menu skirmish setup panel.
- `CurrentSkirmishOptions()` now writes the selected `PlayerFaction` and `AiFaction` into `SkirmishOptions`.
- The menu summary includes both selected factions.
- Added behavior coverage for the UI-facing `SkirmishOptions` faction path and same-faction mirror matches.
- Added `ReviewGate factionselect` so future menu changes cannot silently disconnect faction selection from match setup.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including UI-facing faction loadout and mirror owner-hostility checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj factionselect --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for faction select.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this slice verified setup wiring and deterministic start behavior headlessly; full menu-to-battle runtime automation remains a separate open TODO.

Reviewer result:
- Status: pass
- Required fixes: initial behavior test used a non-existent `GameState.IsHostile` helper; fixed by validating mirror hostility through `FactionRelations.IsHostile`.
- Residual risks: Chinese text entries still have existing mojibake and were not cleaned in this slice; the broader `Boot -> main menu -> skirmish setup -> battle` TODO remains open until runtime UI automation proves the complete flow.

TODO update:
- Items marked done: `Faction select: player picks Dog or Cat at skirmish setup; AI takes the other (or same, for mirror). Relation is owner-based, never faction-based.`
- Items left open: boot-to-battle flow, third faction placeholder, production/economy/combat completion for the full playable slice.
- Reason: main menu selectors now feed `SkirmishOptions`, behavior tests prove faction-specific starts and mirror owner-hostility, and the new ReviewGate locks the wiring.
