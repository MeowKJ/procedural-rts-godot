# BattleRoot vision source buffer review

Step: BattleRoot vision source buffer reuse
Milestone: M9
Owner AI: remote-codex
Reviewer AI: ReviewGate presentation / Integrator
Integrator AI: remote-codex

Scope:
- 为 `BattleRoot.UnitBattlefieldVisionSources` 增加 `(Vector2 Position, float SightRange)` 复用缓冲。
- 用显式填充替换 frame-facing LINQ `Select` iterator。
- 扩展 `BattleRootHudAllocationReviewGate`，通过 `ReviewGate presentation` 锁定 process bridge contract。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，主项目构建通过，0 warnings / 0 errors。
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass，fog vision-source / minimap / mask QA 通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation`
  Result: pass，ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，ReviewGate 0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass，VerifyAll 23/23 通过，包含 FogOfWarQa、PerfSmoke、ActiveBattlePerfQa 和 Godot headless QA。

Reviewer result:
- pass。该 slice 保持 UnitBattlefield vision source 数据来源和 GameState.UpdateWorldOnly contract，只移除 adapter iterator allocation。

Status:
- pass

Residual risks:
- ReviewGate 是文本门禁，不能直接证明运行时分配为零；fog 行为继续由 FogOfWarQa 和 VerifyAll 覆盖。

TODO update:
- TODO.md 的 M9 per-tick allocation paydown 条目已记录 #119 follow-up。
