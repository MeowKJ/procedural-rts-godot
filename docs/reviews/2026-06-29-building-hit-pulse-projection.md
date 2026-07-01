Step: Route live building hit-pulse VFX through UnitBattlefield EntityWorld projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/world/CombatEffectsLayer.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BuildingHitPulseProjection` for live building hit-pulse VFX snapshots.
- Extended `BuildingPresentationProjection` with projected collision radius and EntityWorld `PresentationPulseComponentState.HitPulse`.
- Updated `CombatEffectsLayer` so UnitDesign runtime building hit-pulse rings read `UnitBattlefield.BuildingHitPulseProjections()` instead of legacy `GameState.Buildings`.
- Kept legacy building hit-pulse drawing as the old-runtime fallback.
- Non-goals: no minimap migration, no deletion of `UnitBattlefieldBuildingTarget`, no change to damage math.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior assertions completed successfully, including the new building hit-pulse projection assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildinghitpulseprojection --no-restore`
  Result: pass
  Evidence: building hit-pulse projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: this removes one live presentation dependency on legacy building pulse reads while preserving the old runtime fallback.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingModel.HitPulse` still exists for the legacy runtime and sync fallback.
- CombatEffectsLayer still reads legacy unit pulses and projectile/beams from `GameState`.
- Full `UnitBattlefieldBuildingTarget` removal remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield building hit-pulse projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
