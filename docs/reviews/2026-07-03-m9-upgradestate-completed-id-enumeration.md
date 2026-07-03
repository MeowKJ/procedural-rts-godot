# Review Record - M9 UpgradeState Completed Id Enumeration

Step: #203 `[M9] Reuse UpgradeState completed id enumeration`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / UpgradeStateAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/progression/UpgradeState.cs`, `scripts/core/progression/UpgradeResolver.cs`, `scripts/core/entities/EntityWorld.cs`, `tools/ReviewGateRuntime/UpgradeStateAllocationReviewGate.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`.
- Non-goals: 不改变 upgrade 数值、UI、balance、veterancy 规则、`UpgradeCatalog` 数据或 deterministic hash coverage。

Implementation summary:
- `UpgradeState.CompletedIds` now returns a lightweight `CompletedUpgradeIds` enumerable wrapper instead of `_completed.ToArray()`.
- `UpgradeResolver.ModifierFor(...)` and `EntityWorld.DeterministicStateHash()` keep iterating completed ids in `SortedSet` order without taking an array snapshot.
- `ReviewGate regression` locks the readout against returning to array/list snapshot allocation.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，`upgrade-progression` and full replay suite deterministic。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-upgradestate-completed-id-enumeration`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- `CompletedUpgradeIds` is intentionally enumeration-only; callers needing an owned snapshot must allocate at their boundary explicitly.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
