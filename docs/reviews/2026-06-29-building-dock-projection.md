Step: Route BuildingView refinery dock and pulse visuals through EntityWorld building presentation projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/world/BuildingView.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added delivery pulse and dock occupancy to `BuildingPresentationProjection`.
- Carried `UnitBattlefieldBuildingTarget.DeliveryPulse` through `PresentationPulseComponentState.AlertPulse` during building entity bridging.
- Updated refinery dock visuals and selected-building rally pulse visuals in `BuildingView` to prefer projected data.
- Kept `BuildingModel` delivery/dock/rally pulse fields as old-runtime fallbacks.
- Non-goals: no ResourceSystem rewrite, no refinery gameplay changes, no removal of `BuildingModel`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior assertions completed successfully, including projected dock delivery pulse coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingdockprojection --no-restore`
  Result: pass
  Evidence: building dock projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: refinery dock animation now consumes the same EntityWorld dock and pulse state that gameplay updates, while leaving old-runtime fallbacks intact.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingModel` still stores mirrored dock fields for fallback and transitional sync.
- `BuildingView` still uses legacy building kind/owner for art identity.
- Full `UnitBattlefieldBuildingTarget` removal remains open.

TODO update:
- Marked done: nested M1 slice `BuildingView dock projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
