# Review Record - M9 GameState Fog Source Buffer

Step: m9-gamestate-fog-source-buffer (#138)
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: FogOfWarQa / ReviewGate simhot / self-review
Integrator AI: Remote Linux Codex

Scope:
- 将 legacy `GameState.UpdateFogOfWar(...)` 的 `Where` / `Select` / `Concat` source aggregation 改为 `_legacyFogVisionSources` 可复用 buffer。
- 保留 vision source 顺序：legacy allied units、extra runtime unit sources、allied buildings、signal nodes。
- 调整 `FogOfWarMap.Update(...)`，让 `IReadOnlyList<(Vector2 Position, float SightRange)>` 输入不再被 `ToArray()` 复制。
- 扩展 `GameStateAllocationReviewGate`，锁住 no-`Concat` source aggregation 与 FogOfWarMap no-copy list input contract。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: PASS，0 warnings / 0 errors。
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: PASS，100-source unchanged-source performance smoke 通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: PASS，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: PASS，Errors: 0，Warnings: 0。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: PASS，23/23 steps 全部通过，包含 full ReviewGate、PerfSmoke 与 Godot headless QA。

Reviewer result:
- pass

Status:
- pass

Residual risks:
- `extraUnitSources` 仍是外部 enumerable；本 slice 消除 GameState/FogOfWarMap 内部聚合复制，不改变上游 source producer。
- ReviewGate 是 source-string guard；fog 语义一致性由 FogOfWarQa 和 VerifyAll 覆盖。

TODO update:
- M9 per-tick allocation paydown 保持打开；本 slice 作为 #138 follow-up 记录到 TODO 进度。
