# Review Record - BattleRoot unit culling UnitSpec cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup BattleRoot unit culling slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Integrator / Codex
Reviewer AI: ReviewGate battlerootunitcullingunitspec
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-battleroot-unit-culling-unitspec.md`.
- Non-goals: changing culling cadence, changing camera margins, deleting legacy `UnitKind`, or changing UnitInstance runtime culling.

Implementation summary:
- Replaced the old-runtime unit view culling radius read from `_state.Definition(unit)` with `UnitSpecReadPathFor(...).Descriptor.Radius`.
- The path now reuses the existing BattleRoot UnitSpec bridge from legacy `UnitKind` to authored `UnitSpec` and `UnitSpecRuntimeDescriptor`.
- Added `ReviewGate battlerootunitcullingunitspec` so `RefreshViewCulling()` cannot regress to legacy `GameState` unit definitions for unit radius.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- battlerootunitcullingunitspec`
  Result: pass
  Evidence: ReviewGate battlerootunitcullingunitspec completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=battleroot-unit-culling-unitspec`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow metadata-source migration.
  Result: not run.
  Evidence: culling rect math and camera margin remain unchanged; only radius source changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gate.
- Residual risks: `RefreshViewCulling()` still keeps a legacy `UnitModel` path until the old runtime is deleted.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one scoped live read-path cleanup, not full legacy `UnitKind` deletion.
