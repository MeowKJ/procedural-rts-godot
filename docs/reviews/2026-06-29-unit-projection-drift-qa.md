Step: Add UnitBattlefield UnitInstance projection drift QA as a bounded M1 authority-flip prerequisite.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `UnitProjectionDriftReport` and `UnitBattlefield.UnitProjectionDrift()` to compare legacy `UnitInstance` position/facing against EntityWorld `EntityProjection` state.
- Added deterministic CombatBehavior checks proving non-zero drift is detected and projection sync returns drift to zero.
- Non-goals: no `UseEntityWorldUnits` flag flip, no removal of legacy unit behavior, no view-authority default change.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitprojectiondrift --no-restore`
  Result: pass
  Evidence: Unit projection drift gate completed successfully.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed including the projection drift checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: the drift report is read-only and keeps the current migration behavior intact while giving the future authority flip a measurable guardrail.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- The parent `UseEntityWorldUnits` flip remains open; live unit behavior still syncs EntityWorld from legacy `UnitInstance` during migration.
- The diagnostic is not yet surfaced in a runtime HUD or fail-fast debug mode.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield UnitInstance projection drift QA`.
- Left open: parent M1 unit authority flag flip and legacy behavior deletion.
