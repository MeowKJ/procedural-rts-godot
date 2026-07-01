# Review Record - ControlGroupController UnitSpec bridge

Step: UnitSpec architecture phase 3 duplicate-data cleanup ControlGroupController read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: ControlGroup worker / Codex
Reviewer AI: ReviewGate controlgroupunitspecbridge
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/controllers/ControlGroupController.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-control-group-unitspec-bridge.md`.
- Non-goals: changing control-group input behavior, changing selected ids, moving selection authority, deleting legacy `UnitKind`, or editing building presentation paths.

Implementation summary:
- Replaced old-runtime control group infantry, vehicle, and harvest/economy snapshot buckets with UnitSpec role helpers.
- Legacy `UnitModel.Kind` now resolves through `UnitKindDesignBridge.TryGetSpec(...)`.
- Live `UnitInstance` control-group snapshots and old-runtime fallbacks share harvest/economy semantics from economy/worker role tags plus authored `AbilityKind.Harvest`.
- Added `ReviewGate controlgroupunitspecbridge` to prevent legacy `UnitKind.Harvester`, `UnitKind.Infantry`, or `UnitKind.LightTank` bucket checks from returning to this controller.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- controlgroupunitspecbridge`
  Result: pass
  Evidence: ReviewGate controlgroupunitspecbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=control-group-unitspec-bridge`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow metadata-source migration.
  Result: not run.
  Evidence: snapshot layout and input behavior stay in `ControlGroupController`; only the unit classification data source changed.

Reviewer result:
- Status: pass
- Required fixes: none expected after automated gate.
- Residual risks: legacy control groups still store legacy ids on the old-runtime fallback path until full EntityWorld selection owns gameplay.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and broader control-group EntityWorld selection item remain open.
- Reason: this is one scoped read-path cleanup, not full deletion of legacy compatibility data.
