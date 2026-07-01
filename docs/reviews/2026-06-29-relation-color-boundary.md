# Review Record - Relation color boundary

Step: Keep relation colors out of entity body art and route them through overlays.
Milestone: Architecture hard boundaries.
Owner AI: Codex.
Reviewer AI: Codex self-review (CombatBehavior and ReviewGate provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/FactionVisualPolicy.cs`, `scripts/core/PresentationCatalog.cs`, `scripts/world/UnitView.cs`, `scripts/world/BuildingView.cs`, `scripts/world/UnitInstanceView.cs`, `scripts/core/entities/EntityRenderPalette.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-relation-color-boundary.md`.
- Non-goals: no full art redesign, no owner-color palette redesign, no minimap redesign, no claim that all legacy views are visually final, no change to gameplay hostility rules.

Implementation summary:
- Changed `FactionVisualPolicy.EntityAccent` so entity/body accent is derived from faction accent plus role accent, independent of self/allied/neutral/hostile relation.
- Kept relation colors in `FactionVisualPolicy.RelationOverlay` and relation-sensitive minimap pips.
- Split legacy `UnitView` into `bodyAccent` for `UnitVisualRenderer.DrawUnitSilhouette` and `relationAccent` for command pulse, alert, selection, and health overlays.
- Split legacy `BuildingView` into `bodyAccent` for footprint/structure/production art and `relationAccent` for selection and health overlays.
- Added `ReviewGate relationcolors` to prevent relation color from returning to body/entity accent.
- Updated CombatBehavior checks so same-faction self and hostile entities keep identical body accents while overlays and minimap differ by relation.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including relation-independent body accent checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj relationcolors --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for relation color separation.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this slice changes color routing and is covered by static/behavioral checks; a future UI/art pass should screenshot day/night/mirror-match readability.

Reviewer result:
- Status: pass
- Required fixes: none after adding explicit body-vs-overlay gates.
- Residual risks: legacy `GameState` still uses faction-aware relation helpers for gameplay in some paths; that is tracked by the separate owner-relation hard boundary and remains open. This record only closes the visual relation-color boundary.

TODO update:
- Items marked done: `Relation colors live in overlays (selection/health/minimap/target), never in entity body art.`
- Items left open: owner-relation-only hostility, view authority mutation boundary, pure-presentation effect pooling, full art/UI polish and screenshot QA.
- Reason: code and gates now prove entity/body art stays stable across relation changes, while relation state is carried by overlay/minimap fields.
