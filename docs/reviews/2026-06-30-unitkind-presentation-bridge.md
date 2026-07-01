# Review Record - UnitKind presentation bridge

Step: UnitSpec duplicate-data cleanup UnitKind presentation bridge slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate unitkindpresentationbridge
Integrator AI: Main thread

Scope:
- Files/folders: `scripts/core/units/UnitKindDesignBridge.cs`, `scripts/core/UnitCatalog.cs`, `scripts/core/GameText.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, converting legacy `UnitVisualDescriptor` shapes to `UnitArtRecipe`, changing unit silhouettes, changing production rules, or mapping UnitKinds without UnitDesign files.

Implementation summary:
- Extended `UnitKindDesignBridge` with `CompatibilityPresentation(...)`.
- Bridged UnitCatalog presentation metadata for mapped Dog/Cat UnitKinds through `UnitPresentationCatalog.ForDesign(...)`.
- Kept legacy `UnitVisualDescriptor` as an explicit compatibility input while name key, role key, short code, icon, accent, portrait mode, and role glyph now come from UnitSpec presentation metadata.
- Added English `GameText` keys for the UnitDesign name/role metadata used by bridged legacy presentations.
- Added CombatBehavior assertions that every UnitKind bridge presentation matches UnitSpec presentation metadata while retaining a populated legacy visual descriptor.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully across combat, production, economy, enemy AI, and presentation descriptor checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitkindpresentationbridge --no-restore`
  Result: pass
  Evidence: ReviewGate unitkindpresentationbridge completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice keeps existing legacy visual descriptors and only changes metadata authority.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: legacy UnitCatalog still owns temporary `UnitVisualDescriptor` compatibility shapes until a later art/runtime migration deletes that path.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and legacy UnitKind/UnitCatalog deletion.
- Reason: this is a narrow metadata-authority cleanup slice.
