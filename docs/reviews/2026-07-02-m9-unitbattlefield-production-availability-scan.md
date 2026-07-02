# Review Record - M9 UnitBattlefield Production Availability Scan

Step: #145 `[M9] Replace UnitBattlefield production availability LINQ scan`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate simhot / UnitBattlefieldProductionAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/battlefield/UnitBattlefield.EntityWorldSystems.cs`, `tools/ReviewGateDomains/UnitBattlefieldProductionAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 production queue、roster、tech tier、rally、HUD 或 balance。

Implementation summary:
- `HasAnyProductionForCore(...)` 不再通过 `PlayableDesignIds.Select(UnitDesignCatalog.Spec).Any(...)` 判断生产能力。
- 新路径缓存 producer tech tier，并对 playable design ids 做显式 scan，保持 producer kind / tech-tier 判定一致。
- `ReviewGate simhot` 锁定生产可用性路径不再回到 roster projection/predicate LINQ chain。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot`
  Result: pass。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: 可用性判定仍依赖 UnitDesign roster 数据完整性；该风险继续由 CombatBehavior、PlayerLoopQa 和 roster authoring gates 覆盖。

TODO update:
- Items marked done: none，#10 parent 仍保持打开。
- Items left open: 继续 profiler-guided allocation cleanup。
