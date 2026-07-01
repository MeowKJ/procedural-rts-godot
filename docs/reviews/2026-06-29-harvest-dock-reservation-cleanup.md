Step: Remove immediate legacy refinery dock reservation from UnitBattlefield harvest commands.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed the post-command `ReserveRefineryDock` legacy write from `CommandHarvestSelected`.
- Removed `ReserveRefineryDock` and `RefineryDockLoad`; refinery dock claims are left to `ResourceSystem.ReserveNearestDock` and synced back from `DockComponentState`.
- Non-goals: no ResourceSystem algorithm change, no harvester balance change, no removal of legacy dock display fields.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj harvestdockcleanup --no-restore`
  Result: pass
  Evidence: harvest dock cleanup gate completed successfully.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with harvest/economy scenarios intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 15 steps successfully.

Reviewer result:
- Status: pass.
- Design note: harvest command handling now submits intent only; dock reservation authority stays in the pure ResourceSystem.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `ClearRefineryDockClaim` remains as a legacy sync cleanup helper when commands interrupt harvesting.
- Full resource UI still reads synced legacy fields until the broader runtime deletion phase.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield legacy harvest dock reservation cleanup`.
- Left open: parent M1 behavior deletion until remaining building/unit behavior methods are retired.
