# Entity Framework Architecture

目标：以一个长期可扩展的 RTS 实体框架承载单位、建筑、资源、命令、战斗和胜负状态。

这个文档是给后续 AI 和人工开发看的架构锁定稿。运行时迁移已经完成；后续功能不得重新引入并行权威状态。

## 当前代码状态

live gameplay 只有一条状态链：

```text
MapSpec -> MapLoader.Load -> EntityWorld
PlayerCommand -> CommandGateway -> EntityCommandBuffer -> systems
EntityWorld -> projections/events -> BattleRoot/views/HUD
```

`UnitBattlefield` 持有唯一 `EntityWorld` 和固定模拟时钟，负责命令应用、系统推进和只读查询。普通遭遇战与 authored map 使用相同加载入口。

`BattleRoot` 只负责编排 Godot 节点和表现消费；`WorldPresentationEnvironment` 只管理雾、主题和信号节点，不保存单位、建筑、资源、命令或胜负状态。

```text
UnitDesign / BuildingDesign / MapSpec
                |
                v
EntitySpec -> EntityInstance + component state -> systems
```

单位和建筑仍可有表现投影或 UI descriptor，但这些对象不是模拟权威，也不能回写 world。

## 最终模型

运行时不做继承树。

不要做：

```text
Unit : Entity
Building : Entity
Turret : Building
Projectile : Entity
```

要做：

```text
EntitySpec
- Id
- Kind
- Tags
- Display
- Art
- Components
```

```text
EntityInstance
- EntityId
- SpecId
- OwnerId
- Transform
- ComponentState
```

`EntityKind` 只是分类，不是继承：

```text
Unit
Building
Turret
Resource
Objective
Projectile
Effect
```

实体真正能做什么，由组件决定。

## 运行时身份

实体运行时身份只有：

```text
SpecId + OwnerId + Transform + ComponentState
```

单位和建筑不应该在运行时关心“我是狗狗阵营还是猫猫阵营”。

阵营、战役、科技、沙盒规则只决定：

```text
这个玩家能不能造
什么时候解锁
初始部队是什么
AI 会不会使用
UI 怎么显示可用列表
```

所有者才是运行时关系判断的核心：

```text
OwnerId -> PlayerState
PlayerState -> FactionId
PlayerState -> TeamId
PlayerState -> OwnerColor
```

这样才能自然支持：

```text
狗打狗
猫打猫
狗猫同盟
临时控制权
多人换色
观察者
战役脚本控制单位
```

## 组件边界

推荐第一批组件：

```text
Transform
Health
Selectable
Commandable
Vision
Collision
Movement
WeaponUser
WeaponMountState
ProductionQueue
Construction
Power
ResourceNode
ResourceCargo
Dock
RallyPoint
Objective
BuildRadius
FogRevealer
PresentationPulse
```

系统负责行为：

```text
CommandSystem
MovementSystem
CombatSystem
WeaponSystem
ProjectileSystem
ProductionSystem
ConstructionSystem
ResourceSystem
VisionSystem
SelectionSystem
OutcomeSystem
PresentationEventSystem
```

`EntityInstance` 只保存状态，不承载复杂方法。

## Godot 边界

权威模拟层不要引用 Godot 节点。

规则：

```text
Simulation 不读 SceneTree
Simulation 不依赖 _Process(delta)
Simulation 不依赖动画事件
Simulation 不依赖 Godot 物理回调作为权威
View 不直接改 HP、目标、队列、经济、胜负
View 只提交 Command
```

Godot 负责看起来发生了什么。

Simulation 负责真的发生了什么。

## 命令系统

RTS 手感不要让单位直接响应鼠标点击。

输入应该变成命令：

```text
MoveCommand
AttackCommand
AttackMoveCommand
StopCommand
HoldPositionCommand
BuildCommand
ProduceCommand
RepairCommand
RallyCommand
HarvestCommand
SetStanceCommand
```

框选多个单位时，先生成：

```text
GroupCommand
```

再分配成每个实体的内部目标。

必须分清：

```text
玩家意图点
编队槽位
路径走廊
局部避让目标
UI 可见命令线目标
```

玩家看到的是意图点和清晰命令反馈，不应该看到内部槽位抖动。

## 群体攻击

框选一坨兵攻击一个目标时，不要让所有单位移动到目标中心。

正确模型：

```text
目标中心 -> 根据各单位武器射程生成攻击环
已在射程内 -> 停下开火
未在射程内 -> 找射程环上的可用点
已经开火的单位 -> 成为 combat anchor
后排单位 -> 绕过 combat anchor
位置不完美 -> 接受 good-enough 开火位
```

这样可以避免把正在攻击的单位往前挤。

## 炮塔规则

两种东西要分清：

```text
EntityKind.Turret = 可建造/可选择/可维修/可摧毁的固定平台
WeaponMountSpec = 坦克、飞机、炮塔上会旋转或开火的挂点
```

旋转炮管不是实体。

除非它可以单独：

```text
选择
受伤
维修
摧毁
被占领
```

否则它只是 `WeaponMountSpec + ArtBinding`。

## 建筑规则

普通建筑负责：

```text
生产
经济
电力
科技
建造范围
码头
目标
战役交互
```

防御/支援平台负责：

```text
武器
维修罩
防护罩
扫描场
压制场
```

这些平台内部是 `EntityKind.Turret`，但 UI 可以显示在“防御/炮塔”建造页。

## 美术颜色

实体美术只需要一个所有权颜色：

```text
OwnerColor
```

贴纸不是独立系统。

贴纸就是普通美术层：

```text
ArtLayer(ColorRole.Owner)
```

例如：

```text
坦克侧面条纹
步兵臂章
飞机翼标
建筑门楣
炮塔旋转环
```

推荐颜色角色：

```text
Body
Ink
Owner
Effect
Warning
Shadow
```

敌我关系颜色属于选择框、血条、小地图、目标框、警告线，不属于实体本体。

## 环境色

环境通过 `EnvironmentTone` 影响最终绘制：

```text
base = Palette.Resolve(ColorRole, OwnerColor)
final = EnvironmentTone.Apply(base, layer.EnvironmentResponse)
```

响应类型：

```text
Normal
OwnerProtected
EffectReactive
WarningFixed
```

`Owner` 层必须在白天、雾、夜晚、腐化、隐身、受损状态下保持可读。

## 最终实体架构

实体骨架和固定 tick 管线已经成为 live authority：

```text
EntityKind
EntityId
OwnerId
EntitySpec
EntityInstance
EntityComponentState
EntityWorld
EntityCommand
```

核心代码入口：

```text
scripts/core/entities/EntityKind.cs
scripts/core/entities/EntityId.cs
scripts/core/entities/OwnerId.cs
scripts/core/entities/EntitySpec.cs
scripts/core/entities/EntityInstance.cs
scripts/core/entities/EntityComponentState.cs
scripts/core/entities/EntityWorld.cs
scripts/core/entities/EntityCommand.cs
scripts/core/entities/EntityCommandBuffer.cs
scripts/core/entities/EntityStateHash.cs
scripts/core/entities/UnitEntityFactory.cs
scripts/core/entities/BuildingEntityFactory.cs
scripts/core/units/runtime/UnitBattlefield.cs
scripts/core/units/runtime/battlefield/UnitBattlefield.MapLoading.cs
scripts/core/presentation/WorldPresentationEnvironment.cs
```

`tools/SimReplay`、`PlayerLoopQa`、`CombatBehavior`、地图 handoff QA、Fog QA、SelectionStress 和 ReviewGate 覆盖 spec 转换、地图采用、命令缓冲、稳定顺序、state hash、单位/建筑/资源生命周期以及表现边界。

运行时只允许 `EntityWorld` 持有可变模拟状态；`UnitBattlefield` 负责命令路由、生命周期通知与单向表现投影。测试和 ReviewGate 必须验证当前架构，不得保留第二身份路径、双向状态同步或表现层模拟回写。

## 持续验证切片

先证明这几个实体能工作：

```text
Worker unit
Combat unit
Vehicle factory building
Refinery building
Defense turret
Resource node
Projectile
Objective signal tower
```

验证内容：

```text
生成
选择
移动
攻击
生产
建造
采矿
交付
炮塔开火
建筑摧毁
胜负检测
迷雾视野
OwnerColor 表现
EnvironmentTone 表现
```

## 99 分标准

达到这个标准才算架构定住：

```text
30 个混合单位攻击一个目标不会挤成一团
远程单位停在射程外，近程单位自然前压
正在开火的单位不会被后排推走
建筑、炮塔、单位、资源、目标都能用同一实体语言描述
Godot 节点不参与权威模拟
同一 seed + command log 可以稳定重放
OwnerColor 在所有环境下可读
新增单位或建筑主要是新增 spec，而不是改十几个系统
```

## 已落地的模拟核心（Phase 1-2）

权威核心已经从骨架变成可运行、可重放的系统管线。文件在 `scripts/core/sim/`：

```text
SimClock              固定 30Hz tick；实时 delta 累加成整数 tick，权威模拟只看 FixedDelta
ISimSystem/SimContext 行为单元接口 + 每 tick 上下文（world, tick, fixedDelta, 到期命令）
DeterministicRng      SplitMix64 自有随机源；玩法随机只来自这里；state 折进世界哈希
SimEvent/SimEventSink 模拟产出事件（开火/受伤/死亡），表现层只读 drain，不回写
OwnerRelationTable    运行时敌我判断只认 OwnerId；阵营永不参与
CommandSystem         命令 -> 组件状态的唯一写入点（Move/AttackMove/Attack/Stop/Hold/SetStance）
MovementSystem        直线软到达；射程驻足由 CombatSystem 设置目标
CombatSystem          确定性索敌（稳定 EntityId 平手）、武器旋转、按 WeaponCatalog 计伤 + 种子抖动、死亡事件
EntityProjection/Projector  Simulation -> View 的唯一只读投影边界；视图不碰 EntityInstance
```

`EntityWorld` 现在拥有：系统管线（`AddSystem`/`Step` 稳定顺序）、`OrderedEntities`（稳定 EntityId 迭代）、`QueueRemoval`（tick 结束后按升序删除，避免迭代中改集合）、`Rng`、`Relations`、`Events`。

### 投影边界（新增的优雅约束）

表现层**只能**读 `EntityProjection`（不可变快照），不可触碰 `EntityInstance` 或组件状态。这把
"Simulation owns truth; View owns drawing" 从口号变成类型级约束：

```text
base   = Palette.Resolve(ColorRole, OwnerColor)   // 美术
render = view.Read(EntityProjection)              // 只读投影
command = view.Submit(EntityCommand)              // 只写命令
```

### "新增单位 = 新增 spec" 已被证明

`tools/SimReplay` 的 authored 场景直接用真实的 `dog.infantry` 和 `cat.basic`：
经由 `UnitEntityFactory.SpawnUnit` 把 `UnitDesign -> UnitSpec -> EntitySpec + 组件`，
然后完全由通用 `CombatSystem`/`MovementSystem` 驱动战斗到死亡——**没有任何单位专用代码**。
两遍重放 state hash 一致。这正是 99 分里"新增单位主要新增 spec"的可执行证据。
