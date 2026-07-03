# Review Record - M9 BuildingTarget Production Queue Copy

Step: #191 `[M9] Replace BuildingTargetEntityBridge production queue ToArray`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / BuildingEntityBridgeAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/entities/BuildingTargetEntityBridge.cs`, `tools/ReviewGateDomains/BuildingEntityAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 production queue component 数据结构、enqueue/remove/repeat、production timing、refund、rally behavior、或 component queue array ownership。

Implementation summary:
- `InitialBuildingComponents(...)` now creates `ProductionQueueComponentState` through `CreateProductionQueueItems(...)`.
- The helper allocates the required independent queue item array explicitly and copies `Id`, `Kind`, `DesignId`, `Faction`, and `Progress`.
- `ReviewGate regression` locks the building entity bridge against `productionQueue.ToArray()` returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，SimReplay completed deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，weapon hit rules / turret states / economy / enemy AI / outcomes preserved.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass，build radius / harvest-bank / T1-T3 production / rally / commands / victory-defeat loop preserved.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings；validation tool source budget lock updated to the exact current summary。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-buildingtarget-production-queue-copy`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- The component queue array allocation remains intentional as the immutable component snapshot ownership boundary.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
