# Review Record - Third faction placeholder

Step: Add a locked third-faction placeholder without adding playable content.
Milestone: Playable 1v1 Skirmish - scope locks.
Owner AI: Codex.
Reviewer AI: Codex self-review (ReviewGate and behavior tests provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/FactionId.cs`, `scripts/MainMenuRoot.cs`, `scripts/core/GameText.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-third-faction-placeholder.md`.
- Non-goals: no third-faction units, buildings, loadouts, production, art recipes, AI, or campaign content; no claim that Dog/Cat are fully playable.

Implementation summary:
- Added `FactionId.Corruption` as the reserved third-faction enum slot.
- Added a locked third-faction label to `GameText`.
- Added the third faction to both main-menu faction dropdowns as a disabled item.
- `CurrentSkirmishOptions()` now clamps selected factions through `SelectedPlayableFaction`, using `FactionCatalog.Definitions` so locked factions cannot enter battle setup even if selected programmatically.
- `FactionCatalog` remains Dog/Cat only.
- Added behavior coverage proving the third faction is not registered content and relation logic remains owner-based.
- Added `ReviewGate thirdplaceholder`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including the enum-only locked third-faction check.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj thirdplaceholder --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for the placeholder.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj factionselect --no-restore`
  Result: pass
  Evidence: existing faction-select gate still passes after adding the disabled placeholder.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj skirmishflow --no-restore`
  Result: pass
  Evidence: existing menu-to-battle setup gate still passes after adding the disabled placeholder.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: the locked item is verified structurally and by runtime flow; visual styling polish remains part of the UI/art TODO.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: the broader faction TODO remains open because Dog/Cat full playable completeness still requires production, structure, turret, tier, and balance work.

TODO update:
- Items marked done: `Third faction placeholder`.
- Items left open: `Factions: Dog and Cat fully playable; a third faction exists only as a locked placeholder...` remains open until Dog/Cat full playable criteria are proven.
- Reason: the third faction placeholder portion is now implemented and guarded, while no third-faction content was introduced.
