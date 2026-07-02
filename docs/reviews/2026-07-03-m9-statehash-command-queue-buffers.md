# Review Record - M9 StateHash Command Queue Buffers

Step: #149 `[M9] Reuse EntityStateHash command queue ordering buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / EntityStateHashAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityWorld.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/entities/EntityStateHash.Ordering.cs`, `tools/ReviewGateRuntime/EntityStateHashAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不修改 `CommandQueueComponentState` 数据结构、command dispatch、`EntityCommandBuffer` drain/snapshot 语义或 command payload coverage。

Implementation summary:
- `EntityWorld.DeterministicStateHash()` 增加 `_stateHashCommandQueueItems` 与 `_stateHashCommandSubjectIds`，并传给 `EntityStateHash.Add(...)`。
- `EntityStateHash.AddCommandQueue(...)` 改为填充 caller-owned command item buffer，按 tick/kind 做稳定 in-place sort；每个 command subject 复用 caller-owned subject buffer 并按 `EntityId.Value` 排序。
- `ReviewGate regression` 禁止 command queue hash path 回退到 `OrderBy(...).ThenBy(...)` 和 subject `OrderBy(...)`。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，command replay hash scenarios 仍 deterministic。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: 相同 tick/kind command 的 hash 顺序继续依赖原始 queue order；稳定 sort 与旧 LINQ ordering 保持一致。

TODO update:
- Items marked done: none，#10 parent 仍保持打开。
- Items left open: broader #10 allocation debt remains open beyond this command queue hash slice.
