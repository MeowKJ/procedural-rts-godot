# Review Record - BattleRoot idle-harvest UnitSpec bridge

Step: UnitSpec architecture phase 3 duplicate-data cleanup BattleRoot idle-harvest alert slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Integrator / Codex
Reviewer AI: ReviewGate battlerootidleharvestunitspec
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-battleroot-idle-harvest-unitspec.md`.
- Non-goals: changing alert cooldowns, changing harvester simulation, deleting legacy `UnitKind`, or changing live UnitDesign runtime alerts.

Implementation summary:
- Replaced the old-runtime idle harvester alert's `UnitKind.Harvester` branch with `IsHarvestWorker(unit)`.
- The helper resolves `UnitModel.Kind` through `UnitKindDesignBridge.TryGetSpec(...)`.
- Harvest-worker semantics now use UnitSpec economy/worker role tags plus authored `AbilityKind.Harvest`.
- Added `ReviewGate battlerootidleharvestunitspec` to prevent the legacy harvester kind check from returning to this alert path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- battlerootidleharvestunitspec`
  Result: pass
  Evidence: ReviewGate battlerootidleharvestunitspec completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=battleroot-idle-harvest-unitspec`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow alert classification migration.
  Result: not run.
  Evidence: alert text, cooldown, and branch timing remain unchanged; only the harvester classification source changed.

Reviewer result:
- Status: pass
- Required fixes: none expected after automated gate.
- Residual risks: the old-runtime alert path still depends on legacy `UnitModel.Kind` until full legacy runtime deletion.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one scoped alert read-path cleanup, not full deletion of legacy unit compatibility data.
