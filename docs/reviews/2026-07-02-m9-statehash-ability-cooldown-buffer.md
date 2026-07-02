# Review Record - M9 StateHash Ability Cooldown Buffer

Step: #146 `[M9] Reuse EntityStateHash ability cooldown ordering buffer`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / EntityStateHashAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityWorld.cs`, `scripts/core/entities/EntityStateHash.cs`, `tools/ReviewGateRuntime/EntityStateHashAllocationReviewGate.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`.
- Non-goals: 不处理 production queue、command queue、weapon mount 的排序分配；不修改 ability gameplay。

Implementation summary:
- `EntityWorld.DeterministicStateHash()` 新增 `_stateHashAbilityCooldownValues` 并传给 `EntityStateHash.Add(...)`。
- `EntityStateHash.AddAbilityRuntime(...)` 改为填充 caller-owned cooldown buffer，然后用稳定 insertion sort 按 `AbilityKind` 排序。
- 新增 `EntityStateHashAllocationReviewGate`，把 state-hash allocation evidence 放到 `ReviewGateRuntime`，避免 `ReviewGateDomains` suite 超预算。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression`
  Result: pass。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，deterministic replay scenarios 通过。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: duplicate cooldown kind 理论上不应出现；稳定 insertion sort 保留相同 kind 的原始顺序，避免改变旧 `OrderBy` 的 stable ordering 语义。

TODO update:
- Items marked done: none，#10 parent 仍保持打开。
- Items left open: production queue、command queue、weapon mount 的 state-hash 排序分配仍可继续拆分。
