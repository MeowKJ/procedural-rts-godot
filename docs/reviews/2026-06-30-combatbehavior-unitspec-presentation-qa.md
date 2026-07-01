# Review Record - CombatBehavior UnitSpec presentation QA cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup CombatBehavior presentation QA slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate combatbehaviorunitspecpresentationqa / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-combatbehavior-unitspec-presentation-qa.md`.
- Non-goals: deleting `UnitPresentationCatalog.Units`, deleting `UnitPresentationCatalog.Production`, changing unit balance, changing runtime production behavior, or expanding production enum design.

Implementation summary:
- Moved CombatBehavior roster/presentation coverage off `UnitPresentationCatalog.Units`.
- Moved shared unit presentation completeness checks to `UnitKindDesignBridge.DesignIds`, `UnitPresentationCatalog.ForDesign(...)`, and `UnitPresentationCatalog.ForSpec(...)`.
- Replaced old `UnitVisualDescriptor` body/detail/turret assertions with `UnitArtRecipe` checks for draw layers and owner-color art zones.
- Moved production presentation completeness checks off `UnitPresentationCatalog.Production` enumeration.
- Added faction-aware production checks through `UnitDesignRuntimeLoadouts.ProductionDesignId(...)` and `UnitPresentationCatalog.For(faction, kind)`.
- Added all-playable-design production metadata checks through `UnitPresentationCatalog.ForProductionSpec(...)`.
- Added `ReviewGate combatbehaviorunitspecpresentationqa` to prevent the old CombatBehavior dictionary enumeration from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorunitspecpresentationqa`
  Result: pass
  Evidence: ReviewGate combatbehaviorunitspecpresentationqa completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=combatbehavior-unitspec-presentation-qa`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings after record-format correction.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic tool read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed; tool assertions now read the same presentation intent through UnitSpec entrypoints.

Reviewer result:
- Status: pass
- Required fixes: record format corrected after the first review gate run reported missing template fields.
- Residual risks: `UnitPresentationCatalog.ForProductionSpec(...)` still reads the legacy `Production` compatibility dictionary for tooltip/output-unit compatibility. Legacy presentation compatibility surfaces remain until the broader UnitSpec duplicate-data cleanup deletes them.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes CombatBehavior QA dependence on legacy presentation dictionary enumeration, not the compatibility dictionaries themselves.
