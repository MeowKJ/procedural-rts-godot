# Review Record - m5-projectile-visibility-regression

Step: #76 projectile / 子弹不可见回归修复
Milestone: M5 Unit Progression & Combat Elements
Owner AI: Remote Linux Codex
Reviewer AI: CombatBehavior / ReviewGate simhot / VerifyAll
Integrator AI: Remote Linux Codex

Scope:

- 涉及文件：`scripts/core/presentation/vfx/ProjectileVfxMath.cs`、`ProjectileVfxStyle.cs`、`scripts/core/game-state/GameState.CombatResolution.cs`、`scripts/core/sim/ProjectilePresentationProjection.cs`、`scripts/world/CombatEffectsLayer.CombatDraw.cs`、`CombatEffectsLayer.Visibility.cs`、`scripts/BattleRoot.Lifecycle.cs`、`tools/CombatBehavior*`、`tools/ReviewGateDomains/RegressionReviewGate.cs`、`TODO.md`。
- 目标：让旧 `GameState.Projectiles` 与新 ECS projectile projection 共用同一套可读 projectile style，并确保正常战斗视角下 ordinary projectile 与 seeker/tracking projectile 不会因为线宽、alpha、culling、FogOfWarLayer 顺序而完全消失。
- 非目标：不修改 projectile 伤害、速度、追踪、命中规则、射程、平衡数值，也不重做整套攻击特效系统。

## 实现摘要

- 新增 `ProjectileVfxMath.StyleFor(...)` 与 `ProjectileVfxStyle`，集中管理 tail/core/head 尺寸、alpha、culling padding 和 seeker tail flare。
- `GameState.CombatResolution` 创建 legacy projectile 时从 shared style 初始化 `TrailWidth`、`CoreWidth`、`HeadRadius`，移除旧的局部三元表达式。
- `ProjectilePresentationProjection` 为 ECS projectile 携带 shared style，`CombatEffectsLayer` 直接使用 `projectile.Style` 绘制。
- `CombatEffectsLayer.DrawProjectile` 改为 segment culling，并在绘制到 fog 上方时通过 `IsProjectileVisibleToPlayer(...)` 保持 FogOfWar 可见性边界。
- `BattleRoot` 先添加 `FogOfWarLayer`，再添加 `CombatEffectsLayer`，让 live-visible projectile 不被 fog overlay 吞掉。
- `CombatBehavior` 增加 legacy projectile style 与 ECS seeker projection style 断言；`ReviewGate simhot` 锁定 shared style、segment culling、fog visibility gate 和层级顺序。

Automated gates:

- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: 通过，0 warnings / 0 errors。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: 通过，覆盖 legacy projectile style 初始化和 ECS seeker projection style。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: 通过，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: 通过，0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: 通过，23/23 steps passed；包含 Godot battle headless、SkirmishFlowQa、ActiveBattlePerfQa、PauseQa。

Reviewer result:

Status: pass

Residual risks: 本次是代码级与 headless QA 证据，没有人工截图逐帧检查每个 zoom 档位；不过 shared style minima、segment culling、fog visibility gate 和 headless battle/perf QA 已经覆盖本次回归的主要链路。

TODO update:
- 已在 M5 projectile/ammo 段落记录 #76 回归修复、shared projectile style、fog visibility gate、layer order 和通过的验证门。
