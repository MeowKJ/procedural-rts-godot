# Review Record - ColorRole collapse

Step: Collapse legacy `ColorUse` art colors into the canonical `ColorRole` system.
Milestone: UI & presentation polish / visual style architecture.
Owner AI: Codex.
Reviewer AI: Codex self-review (CombatBehavior and ReviewGate provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/ArtLayer.cs`, `scripts/core/ColorUse.cs`, `scripts/core/UnitRenderPalette.cs`, `scripts/core/UnitVisualRenderer.cs`, `scripts/core/entities/EntityRenderPalette.cs`, `scripts/core/units/dog/DogUnitArt.cs`, `scripts/core/units/cat/CatUnitArt.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-color-role-collapse.md`.
- Non-goals: no full palette redesign, no HUD/world palette unification, no screenshot pass, no gameplay behavior change.

Implementation summary:
- Removed the legacy `ColorUse` enum file.
- Changed `ArtLayer` to store `ColorRole` directly.
- Updated dog and cat `UnitArtRecipe` layers to use `ColorRole.Body`, `Ink`, `Owner`, and `Effect`.
- Updated `UnitVisualRenderer` and `UnitRenderPalette` to resolve `ColorRole` directly.
- Removed `EntityRenderPalette.ResolveLegacy` and `ColorRoleMapping`.
- Added `ReviewGate colorroles` to reject future `ColorUse`, `ResolveLegacy`, `ColorRoleMapping`, and `ColorRoleOverride` usage in code.
- Repaired the CombatBehavior zh-CN localization assertions with Unicode escape literals so the test no longer depends on mojibake text.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed after direct ColorRole migration and localization assertion repair.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj colorroles --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for ColorRole collapse.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this is a mechanical art-role API migration; visual screenshot QA remains open under the broader readability/art TODOs.

Reviewer result:
- Status: pass
- Required fixes: initial mechanical replacement exposed an old mojibake localization assertion in CombatBehavior; replaced those expected zh-CN strings with Unicode escapes.
- Residual risks: `UnitRenderPalette` still exists as a compatibility palette for legacy rendering, and full world/HUD palette unification remains open. This slice only removes `ColorUse` and makes art layers bind directly to `ColorRole`.

TODO update:
- Items marked done: `EntityRenderPalette.Resolve(ColorRole, OwnerColor, EnvironmentTone); collapse ColorUse -> ColorRole` and `Color roles (collapse ColorUse -> ColorRole)`.
- Items left open: one canonical palette source shared by world + HUD, environment tone full polish, faction shape language, silhouette readability, owner-color zones, visual QA.
- Reason: current code no longer defines or consumes `ColorUse`; unit art, render palettes, and renderers now use `ColorRole` directly with automated gates to keep it that way.
