# Review Record - M9 StateHash Production Queue Buffer

Step: #148 `[M9] Reuse EntityStateHash production queue ordering buffer`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / EntityStateHashAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityWorld.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/entities/EntityStateHash.Ordering.cs`, `tools/ReviewGateRuntime/EntityStateHashAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不修改 `ProductionQueueComponentState` 数据结构、enqueue/remove 行为、生产 timing、refund 或 repeat 语义。

Implementation summary:
- `EntityWorld.DeterministicStateHash()` 增加 `_stateHashProductionQueueItems`，并传给 `EntityStateHash.Add(...)`。
- `EntityStateHash.AddProduction(...)` 改为填充 caller-owned queue item buffer，然后按 item id 做稳定 in-place sort，保持旧 `Items.OrderBy(item => item.Id)` 的 hash 顺序。
- `ReviewGate regression` 禁止 production queue hash path 回退到 `state.Items.OrderBy(item => item.Id)`。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，production replay hash scenarios 仍 deterministic。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，combat / production / economy coverage 通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: duplicate production item id 不应出现；稳定 sort 会保留相同 id 的原始顺序。

TODO update:
- Items marked done: none，#10 parent 仍保持打开。
- Items left open: broader #10 allocation debt remains open beyond this production queue hash slice.
