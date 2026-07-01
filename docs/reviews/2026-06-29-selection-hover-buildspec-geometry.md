Step: Route SelectionController building hover fallback geometry through BuildSpecCatalog as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/controllers/SelectionController.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated old-runtime building hover affordance radius to derive from `BuildSpecCatalog.For(building.Kind).Footprint`.
- Removed the fallback dependency on `State.CombatTargetRadius(CombatTargetKind.Building, ...)` for hover geometry.
- Non-goals: no live `UnitBattlefield` hover projection changes, no picking changes, no legacy combat geometry rewrite.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj selectionhoverbuildspecfallbacks --no-restore`
  Result: pass
  Evidence: SelectionController hover BuildSpec fallback gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: old-runtime hover drawing now uses BuildSpec geometry, matching the building hit-pulse and minimap fallback direction.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Legacy picking and combat systems still have internal building-radius helpers until entity authority fully replaces old gameplay paths.
- This slice does not change live `UnitBattlefield` hover projection behavior.

TODO update:
- Marked done: nested M1 slice `SelectionController building hover BuildSpec geometry cleanup`.
- Left open: parent M1 legacy deletion and broader old-runtime authority retirement.
