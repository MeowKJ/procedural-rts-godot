Step: Route live power alerts through UnitBattlefield EntityWorld power projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `UnitBattlefieldPowerStatusProjection` for owner-level power snapshots.
- Added `UnitBattlefield.PowerStatus(...)`, which sums active EntityWorld `PowerComponentState` providers and consumers for a player slot.
- Ignored dead or unfinished power entities when computing live power status.
- Updated `BattleRoot.UpdatePowerAlert` so UnitDesign runtime power alerts use UnitBattlefield power projections, while the legacy `State.Buildings` PowerPlant check remains only as the old-runtime fallback.
- Non-goals: no HUD copy rewrite, no PowerSystem refactor, no deletion of `UnitBattlefieldBuildingTarget`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior assertions completed successfully, including the new power projection assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingpowerprojection --no-restore`
  Result: pass
  Evidence: building power projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: the live path now follows component power budgets instead of a hard-coded PowerPlant presence check.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- The old-runtime fallback still uses `State.Buildings` and `BuildingKind.PowerPlant`.
- Live power status is projected from UnitBattlefield's EntityWorld mirror; full removal of `UnitBattlefieldBuildingTarget` remains open.
- Power alert copy still says stable/offline only; richer low-power wording is a later UI task.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield building power-alert projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
