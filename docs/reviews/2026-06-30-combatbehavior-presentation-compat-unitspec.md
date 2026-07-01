# Review Record - CombatBehavior presentation compatibility UnitSpec cleanup

Step: CombatBehavior presentation compatibility UnitSpec cleanup
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate combatbehaviorpresentationcompatunitspec / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-combatbehavior-presentation-compat-unitspec.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, deleting `UnitPresentationCatalog`, changing production presentation QA, changing balance/stats, or changing runtime behavior.

Implementation summary:
- Removed the remaining `UnitPresentationCatalog.For(kind)` calls from CombatBehavior compatibility presentation QA.
- Mapped legacy `UnitKind` presentation checks now resolve through `UnitKindDesignBridge.DesignId(...)` and `UnitPresentationCatalog.ForDesign(...)`.
- Generic legacy UnitKind compatibility still validates runtime descriptor projection and UnitSpec presentation metadata without reading legacy UnitKind presentation descriptors.
- Added `ReviewGate combatbehaviorpresentationcompatunitspec` to keep CombatBehavior off the legacy UnitKind presentation read path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorpresentationcompatunitspec`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings after updating older bridge gate expectations to the new UnitSpec read path.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=combatbehavior-presentation-compat-unitspec`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: deterministic tool-side QA read-path cleanup only.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `UnitPresentationCatalog.For(UnitKind kind)` still exists as a compatibility API until the broader legacy presentation surface can be deleted.

TODO update:
- Items marked done: `CombatBehavior presentation compatibility UnitSpec cleanup` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: broad UnitSpec duplicate-data cleanup and final `UnitKind` / `UnitCatalog` deletion remain open.
- Reason: the tool no longer exercises legacy UnitKind presentation reads, but the compatibility API still exists.
