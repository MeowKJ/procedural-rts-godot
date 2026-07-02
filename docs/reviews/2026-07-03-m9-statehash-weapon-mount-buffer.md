# Review Record - M9 StateHash Weapon Mount Buffer

Step: #147 `[M9] Reuse EntityStateHash weapon mount ordering buffer`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / EntityStateHashAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityWorld.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/entities/EntityStateHash.Ordering.cs`, `tools/ReviewGateRuntime/EntityStateHashAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不修改 `WeaponUserComponentState` 数据模型、战斗语义、目标选择或 cooldown 行为。

Implementation summary:
- `EntityWorld.DeterministicStateHash()` 增加 `_stateHashWeaponMountValues`，并传给 `EntityStateHash.Add(...)`。
- `EntityStateHash.AddWeaponUser(...)` 改为填充 caller-owned mount buffer，然后按 `MountId` 做稳定 in-place sort，保持旧 `OrderBy(..., StringComparer.Ordinal)` 的 hash 顺序。
- `EntityStateHash.Ordering.cs` 承载 collection hashing helpers，避免主文件继续膨胀。
- `ReviewGate regression` 禁止 weapon mount hash path 回退到 `state.Mounts.OrderBy(...)`。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，deterministic replay scenarios 通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: duplicate `MountId` 理论上不应出现；稳定 insertion sort 会保留相同 key 的原始顺序，避免改变旧 LINQ stable ordering 语义。

TODO update:
- Items marked done: none，#10 parent 仍保持打开。
- Items left open: broader #10 allocation debt remains open beyond this weapon mount hash slice.
