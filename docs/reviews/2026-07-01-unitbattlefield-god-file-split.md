# Review Record - UnitBattlefield god-file split

Step: Split UnitBattlefield into focused partial companion files
Milestone: Single responsibility - god-class breakup
Owner AI: Codex
Reviewer AI: Build / ReviewGate / runtime smoke
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefield.*.cs`, `scripts/core/units/runtime/UnitProjectionDriftReport.cs`, `tools/ReviewGate/FileSizeGate.cs`, `tools/ReviewGate/ReviewGateChecks.Part*.cs`, `TODO.md`.
- Non-goals: changing runtime behavior, deleting legacy compatibility paths, converging the transitional combat systems, or changing UI/gameplay balance.

Implementation summary:
- Converted `UnitBattlefield` to a partial class and kept the update loop plus shared fields in the small `UnitBattlefield.cs` entry file.
- Split responsibilities into companion files for core queries, harvest/repair, building lifecycle, building projection/sync/state, selection/picking, production/rally, commands, visibility/combat bridge logic, EntityWorld system stepping, command bridge/application, runtime sync, and legacy utility helpers.
- Moved `UnitProjectionDriftReport` into its own small file.
- Updated ReviewGate's `UnitBattlefield.cs` readers to use `ReadSourceWithPartials(...)`, preserving historical checks across the partial family.
- Removed `scripts/core/units/runtime/UnitBattlefield.cs` from the known red-line file-size debt whitelist.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate project compiled with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and expected file-size debt/watchlist warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: known red-line debt warning dropped to 9 files over 600 lines after the split.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: this is a mechanical responsibility split; deeper deletion of legacy runtime compatibility paths remains separate TODO work.

TODO update:
- Items marked done: `UnitBattlefield god-file split`.
- Items left open: final legacy enum/catalog deletion, combat convergence, and other red-line files.
- Reason: the live runtime facade is no longer a 3991-line God class and cannot silently regrow under the file-size gate.
