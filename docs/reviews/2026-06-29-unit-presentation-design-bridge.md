Step: Add a safe UnitSpec presentation bridge while preserving legacy UnitKind presentations.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/UnitPresentationCatalog.cs`, `scripts/core/UnitSpecPresentationDescriptor.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `UnitPresentationCatalog.ForDesign(string designId)` and `ForSpec(UnitSpec spec)` as UnitSpec-driven presentation entrypoints.
- Added `UnitSpecPresentationDescriptor` carrying UnitSpec metadata/art: spec id, name key, role key, short code, icon, portrait mode, accent, art recipe, and role glyph.
- Kept legacy `UnitPresentationCatalog.Units => UnitCatalog.Presentations` unchanged for existing `GameState`, localization, production, and faction compatibility.
- Non-goals: deleting `UnitCatalog`, deleting `UnitKind`, changing `FactionCatalog`, migrating `GameState`, changing unit values, or replacing legacy UnitKind presentation descriptors.

Implementation summary:
- The new bridge resolves design ids through `UnitDesignCatalog.Spec(designId)` and projects directly from `UnitSpec`.
- `ForSpec` derives accent from `SoftOldCityPalette.FactionColor(spec.Faction)` and role glyph from `spec.Art.StatusGlyph` with an icon fallback.
- `CombatBehavior` now asserts Dog/Cat UnitDesign presentation metadata and owner-color art are exposed without migrating legacy UnitKind presentations.
- `ReviewGate unitpresentationdesignbridge` verifies the new entrypoints exist, the descriptor is legacy-free, the bridge method bodies do not read `UnitCatalog` or `UnitKind`, and legacy `Units` still points at `UnitCatalog.Presentations`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitpresentationdesignbridge`
  Result: pass
  Evidence: dedicated bridge gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj`
  Result: pass
  Evidence: CombatBehavior completed with presentation descriptor and UnitSpec bridge assertions.

Manual/visual gates:
- Check: visual inspection not required for this data-entrypoint slice.
  Result: not run.
  Evidence: no UI rendering path was changed; legacy `Units` rendering data remains unchanged.

Reviewer result:
- Status: pass.
- Required fixes: none identified after automated gates.
- Residual risks: this is an owner-side review record, not an independent reviewer sign-off; legacy `UnitPresentationCatalog.Units`, `UnitCatalog`, `UnitKind`, `GameState`, and `FactionCatalog` remain in place for later migration slices.

Status:
- Pass.

Residual risks:
- The broad duplicate-data TODO remains open.
- Existing UnitDesign translation keys are not forced through this bridge yet.
- Call sites must opt into `ForDesign` / `ForSpec`; no old UnitKind caller was migrated in this slice.

TODO update:
- Items marked done: none.
- Items left open: parent duplicate unit definition cleanup for `GameState`, `UnitPresentationCatalog`, and `FactionCatalog`.
- Reason: this slice adds a safe UnitSpec presentation entrypoint and evidence gate while preserving legacy `Units` compatibility.
