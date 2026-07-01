Step: Route live building combat alerts through UnitBattlefield target/death data as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated live building under-attack alerts to use `UnitBattlefieldBuildingTarget` player slot, position, kind, and `BuildSpecCatalog` label data.
- Updated live building destroyed alerts to use `UnitBattlefieldBuildingDeathInfo` player slot, position, kind, and `BuildSpecCatalog` label data.
- Kept legacy `BuildingModel` health/hit-pulse mirroring as a temporary fallback sync only.
- Non-goals: no alert UI redesign, no event bus refactor, no deletion of `UnitBattlefieldBuildingTarget`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior assertions completed successfully, including self-contained building combat/death alert data coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingcombatalertprojection --no-restore`
  Result: pass
  Evidence: building combat alert projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: live alerts no longer need legacy `BuildingModel` owner/position/label lookups to tell the player what happened.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingModel` still mirrors HP/hit pulse for legacy UI fallback.
- Building view removal still relies on `_buildingViews` keyed by legacy id.
- Full `UnitBattlefieldBuildingTarget` removal remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield building combat-alert projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
