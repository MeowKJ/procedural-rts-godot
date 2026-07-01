# 巨大改革计划 — 三系统收敛为单一确定性实体框架

Status: Historical phase plan and progress record. Current execution should be
driven by root `TODO.md`, `EntityFrameworkArchitecture.md`, `RTS99Design.md`, and `FileStructureGovernance.md`.

## 诊断（为什么必须改）

代码现在有三套并存的实体系统，正是 `EntityFrameworkArchitecture.md` 警告的"两个 God Object"局面：

| 系统 | 文件 | 行数 | 状态 |
|---|---|---|---|
| 旧 Legacy | `GameState` + `UnitModel`/`BuildingModel`/`*Kind`/`*View` + `BuildingDefinition`/`BuildDefinition` | GameState 3321 | 仍拥有建筑、经济、迷雾、生产、投射物 |
| 新单位 | `UnitDesign→UnitSpec→UnitInstance→UnitBattlefield→UnitInstanceView` | UnitBattlefield 1766 | **当前实际运行路径** (`UseUnitDesignRuntime=true`)，但已长成第二个 God Object（含 `UnitBattlefieldBuildingTarget` 第二套建筑运行时） |
| 目标 ECS | `scripts/core/entities/*` | 1019 总计 | 有骨架+桥+测试，但**驱动零个实时玩法** |

其他硬伤：
- 实时路径无固定 tick / 无 command buffer（`Update(double delta)` 直接吃 Godot 实时 delta）。`EntityCommandBuffer` 存在但实时未用。
- Sim/View 边界仅文档化，未在代码强制（`GameState` import Godot，被 Node 流程驱动）。
- 建筑完全是 legacy，再镜像进 `UnitBattlefieldBuildingTarget` → 两套建筑运行时。

## 框架（落地为 C# 的具体形态）

权威设计来自你的两份文档，本计划把它固化成可编译的目标结构：

```
ProceduralRts.Sim         (无 Godot 节点依赖；可被 headless 工具直接跑)
  EntityWorld             权威世界：实体、组件存储、系统调度、StateHash
  EntitySpec / EntityInstance / EntityComponentSet
  Systems/                CommandSystem, MovementSystem, CombatSystem,
                          WeaponSystem, ProjectileSystem, ProductionSystem,
                          ConstructionSystem, ResourceSystem, VisionSystem,
                          SelectionSystem, OutcomeSystem, PresentationEventSystem
  Commands/               EntityCommand 子类 + EntityCommandBuffer（实时启用）
  SimClock                固定 tick（如 30Hz），累积实时 delta → 整数 tick

ProceduralRts.Authoring   UnitDesign/BuildingDesign/TurretDesign… → EntitySpec
ProceduralRts.Presentation  EntityView/WorldLayer/HudLayer：只读快照、只提交 Command
```

## 执行 — Phase 1（本次会话目标，strangler 增量）

目标：让 `EntityWorld` + 固定 tick + CommandBuffer 成为**真正驱动一类实体的实时核心**，
游戏在每个检查点都能 boot。不删任何 legacy。

1. **SimClock**：新增固定 tick 累加器，`BattleRoot` 把实时 delta 喂给它，sim 按整数 tick 步进。
2. **EntityWorld 系统调度**：给现有 `EntityWorld` 加 `Step(tick, CommandBuffer)`，按稳定 `EntityId` 顺序跑系统列表。先实现 `CommandSystem` + `MovementSystem`。
3. **命令入口**：`SelectionController`/输入把玩家意图转成 `MoveCommand`/`SelectCommand` 投入 `EntityCommandBuffer`，不再直接 mutate。
4. **一类实体真正跑通**：把 worker + combat unit 的**移动**从 `UnitBattlefield` 改为经由 `EntityWorld.Step`（其余行为暂留 UnitBattlefield，桥接同步），用 `UseEntityWorldMovement` flag 控制。
5. **确定性验证**：扩展 `tools/SimulationSmoke` 或新增 `tools/SimReplay`：同 seed + 同 command log 跑两次，断言 `EntityStateHash` 相同 N 千 tick。
6. 每步 `dotnet build` + 相关 smoke 工具通过；headless `Battle.tscn` 启动 3 秒不崩。

## 验收（Phase 1 完成定义）

- 游戏照常 boot，单位移动手感不退化（移动现在跑在 EntityWorld 上）。
- `dotnet build` 通过；SimReplay 工具两次哈希一致。
- 写一段架构注释/笔记记录新边界与 flag 含义。
- 更新 `TODO.md`：勾掉 Migration 1-2 相关项，标注 Phase 1 进度。

## 后续 Phase（不在本次执行，列出顺序供确认）

- P2 单位全行为（战斗/采矿/生产映射）迁上 EntityWorld，退役 UnitBattlefield 行为方法
- P3 建筑：合并 `BuildingDefinition`+`BuildDefinition`，`UnitBattlefieldBuildingTarget` → 实体组件，删第二套建筑运行时
- P4 命令锁：GroupCommand/攻击找位/firing anchor 全走 CommandBuffer + replay
- P5 视觉锁：`ColorRole`/`OwnerColor`/`EnvironmentTone` 统一 ArtRecipe
- P6 删 legacy：`UnitKind`/`BuildingKind`/`GameState.Definitions`/`UnitCatalog`

---

## 进度记录

### Phase 1 完成（确定性模拟核心骨架）

- `scripts/core/sim/`：`SimClock`（固定 30Hz tick）、`ISimSystem`/`SimContext`、`MovementProfileComponentState`
- `EntityWorld` 扩展：`AddSystem`/`Step`/`OrderedEntities`（稳定 EntityId 顺序）
- `CommandSystem` + `MovementSystem`（直线软到达）
- `BattleRoot.StepEntityWorld` 把固定时钟接入实时引擎（非权威接缝）
- `tools/SimReplay`：30 单位 × 6000 tick 跑两遍 state hash 一致

### Phase 2 进行中（命令词汇 + 确定性战斗）

已完成：
- `DeterministicRng`（SplitMix64）：EntityWorld 自有随机源，state 折进世界哈希
- `SimEvent`/`SimEventSink`：模拟产出事件，表现层 drain 只读不回写
- `OwnerRelationTable`：运行时敌我判断只认 OwnerId，阵营不参与
- `StanceComponentState`：实体路径的交战姿态
- 命令词汇扩展：Move / AttackMove / Attack / Stop / HoldPosition / SetStance（`EntityCommand` + `CommandSystem` 单一写入点）
- `CombatSystem`：确定性索敌（稳定 EntityId 平手判定）、武器旋转、射程驻足、按目录 ammo/armor 计算伤害 + 种子化抖动、死亡事件 + 延迟移除（`EntityWorld.QueueRemoval`，tick 结束后按升序刷新）
- `EntityWorld.Step` 在系统跑完后 flush 移除，避免迭代中改集合
- `BattleRoot` 注册 Command+Combat+Movement 管线，镜像 player/enemy 敌对关系
- `tools/SimReplay` 新增 24 单位双阵营战斗场景（4000 tick，含种子伤害和死亡），两遍 hash 一致；旧 CombatBehavior/SimulationSmoke 回归通过；headless Battle 启动无 sim 崩溃

待办（P2 收尾）：把实时 UnitBattlefield 的单位移动/战斗真正切换到 EntityWorld（当前 EntityWorld 仍为非权威影子；需要把 UnitInstance 生成同步进 EntityWorld 并由其驱动渲染位置），再退役 UnitBattlefield 行为方法。

### Phase 2.5 完成（群体命令 + RTS 手感锁，纯确定性）

- `EntityProjection`/`EntityProjector`：Simulation→View 唯一只读投影边界
- `UnitSpecEntityBridge.SpawnUnit` 产出完全可模拟实体（authored 单位零专用代码）
- `GroupMoveEntityCommand`/`GroupAttackEntityCommand`：一个玩家意图 → 分解为每实体槽位
- `FormationMath`（复用）做群体移动紧凑槽位；`AttackSlotMath`（新增）做射程环找位 + firing anchor
- 可见命令线指向意图点，内部 FormationSlot 不外露（手感分层）
- `tools/SimReplay` 新增 5 个场景全部确定性通过：
  - movement / combat / authored(dog-vs-cat) / **group-move(30 单位最小间距 52px 不挤)** / **group-attack(30 单位环绕目标，0 个堆中心，全在射程带)**

这达成了 99 分验收口径里最核心的两条："框选 30 个单位攻击不挤成一团" + "远程单位停在射程外"。

### Phase 2.6 完成（避让/分离 + 视野 + 胜负 + 指标）

- `SeparationSystem`：严重碰撞位置分离(站立/开火单位为不可推动锚点),群移途中最小间距从 5.1px 升到 23.6px,无重叠无绕圈,确定性
- `MovementSystem`：叠加软避让(目标吸引为主,复用 `SpatialGrid<LocalAvoidanceBody>` + `LocalAvoidanceMath`)
- `VisionSystem` + `VisibilityIndex`：每 tick 按友方视野计算 per-owner 玩法可见集(与视觉雾分离)
- `OutcomeSystem` + `ObjectiveComponentState`：通用胜负判定(实体标记 victory-critical,不写死 HQ)
- `SimMetrics`：纯事件派生只读指标(shots/kills/damage/time-to-first-shot),系统保持无状态
- 完整权威管线 Command→Combat→Movement→Separation→Vision→Outcome 已在 `BattleRoot` 注册
- `tools/SimReplay` 现有 6 个确定性场景全过:movement / combat(+指标) / authored / group-move(避让) / group-attack / outcome(视野+胜负)

至此模拟核心脊椎完整:固定 tick、命令缓冲、确定性 RNG、事件、关系、投影、战斗、移动、避让、分离、视野、胜负、指标——全部 Godot-free 且可重放。下一步是让 `EntityWorld` 真正接管实时单位(P2 收尾),退役 `UnitBattlefield` 行为方法。
