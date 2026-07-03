# Review Record - M9 Building Presentation Queue Copy

Step: #192 `[M9] Replace building presentation queue clone LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / BuildingEntityBridgeAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/sim/BuildingPresentationProjector.Queue.cs`, `tools/ReviewGateDomains/BuildingEntityAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 building projection fields、HUD display、production queue ordering、production behavior、rendering style、或 presentation snapshot ownership。

Implementation summary:
- `BuildingPresentationProjector.ProjectOne(...)` now calls `CloneProductionQueue(...)` instead of `production.Items.Select(CloneQueueItem).ToArray()`.
- The queue clone helper lives in `BuildingPresentationProjector.Queue.cs` to keep the main projection file stable and uses an indexed copy loop.
- `ReviewGate regression` locks the building presentation projector against LINQ queue clone regressions.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，weapon hit rules / turret states / economy / enemy AI / outcomes preserved.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass，1280x720 / 1600x900 / 1920x1080 / high-DPI HUD constraints preserved.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings；validation tool source budget lock updated to the exact current summary。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-building-presentation-queue-copy`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- The returned presentation queue array allocation remains intentional as the read-only presentation snapshot ownership boundary.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
