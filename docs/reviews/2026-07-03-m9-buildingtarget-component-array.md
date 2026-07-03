# Review Record - M9 BuildingTarget Component Array

Step: #193 `[M9] Replace BuildingTargetEntityBridge component yield array`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / BuildingEntityAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/entities/BuildingTargetEntityBridge.cs`, `tools/ReviewGateDomains/BuildingEntityAllocationReviewGate.cs`.
- Non-goals: 不改变 component 顺序、字段值、spawn contract、production、weapon、dock、rally 或 power behavior。

Implementation summary:
- `BuildingTargetEntityBridge` now builds initial building components through `CreateBuildingComponents(...)`.
- The helper allocates the required `EntityComponentState[]` explicitly and fills it in the same order as the previous iterator.
- `BuildingEntityAllocationReviewGate` locks the bridge against iterator-based component construction and `ToArray()` materialization.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，deterministic replay scenarios preserved。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，turret/building combat and production behavior preserved。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass，build / harvest / production / rally / command / outcome loop preserved。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-buildingtarget-component-array`
  Result: pass，record is present with concrete automated gate evidence。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- The component array allocation remains intentional as the independent bridge snapshot ownership boundary.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
