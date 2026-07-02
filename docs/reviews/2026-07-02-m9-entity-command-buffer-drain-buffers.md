# Review Record - m9-entity-command-buffer-drain-buffers

Step: #77 EntityCommandBuffer drain buffer reuse
Milestone: M9 Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: SimReplay / CombatBehavior / ReviewGate simhot
Integrator AI: Remote Linux Codex

Scope:
- 涉及文件：`scripts/core/entities/EntityCommandBuffer.cs`、`tools/ReviewGateDomains/CommandSystemAllocationReviewGate.cs`、`TODO.md`、`docs/reviews/2026-07-02-m9-entity-command-buffer-drain-buffers.md`。
- 目标：复用 `EntityCommandBuffer` 的 ordered snapshot、ready command、ready sequence buffers，移除 `DrainUpToTick(...)` 的 LINQ ready list / sequence set allocation。
- 非目标：不修改 command tick / issuer / sequence ordering，不修改 command application systems，不处理 Ability cooldown immutable array debt，不关闭整个 #10。

Implementation summary:
- `EntityCommandBuffer` 新增 `_snapshotBuffer`、`_readyBuffer`、`_readySequences`。
- `DrainUpToTick(...)` 通过 `CopyOrderedCommandsInto(...)` 排序到复用 buffer，再填充 ready buffer 和 ready sequence set，最后用 `RemoveAll(IsReadySequence)` 删除已 drain command。
- `Snapshot()` 保持公开 API，仍返回稳定数组 snapshot；hot drain path 不再调用 LINQ `Where(...).ToList()` / `Select(...).ToHashSet()`。
- `CommandSystemAllocationReviewGate` 新增 `EntityCommandBuffer` drain buffer evidence，并由 `ReviewGate simhot` 统一执行。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: 通过，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: 通过，replay hashes 保持确定。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: 通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: 通过，0 errors / 0 warnings。
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: 通过，400-unit worst average 11.349ms，alloc/tick 192620。

Reviewer result:

Status: pass

Residual risks: `Snapshot()` 为保持稳定公开 API 仍会返回数组 snapshot；本切片只消除 hot drain path 的 ready list / sequence set LINQ allocation。Ability cooldown arrays 和其他 immutable queue/path debt 仍属于 #10 后续工作。

TODO update:
- 已在 M9 allocation paydown 段落记录 #77 的 buffer reuse 和 `ReviewGate simhot` no-LINQ drain contract。
