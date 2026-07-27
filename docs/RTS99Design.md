# RTS 99 Design

目标：把当前 Godot/C# RTS 原型迭代成一个长期可扩展、手感稳定、能支持战役/沙盒/未来联机的 99 分框架。

这不是美术设定稿，也不是单个系统的实现说明。它是整款 RTS 的设计约束和验收标准。

## 研究校准

本设计参考这些方向：

```text
Godot nodes/scenes: 节点适合组织表现和场景，但权威模拟需要自己的数据边界。
Deterministic lockstep: RTS 联机/回放应从 command log、固定 tick、确定性状态推进开始设计。
Steering behaviors: 群体单位需要目标吸引、分离、避让、路径跟随等局部行为组合。
ORCA / RVO: 多智能体避让的核心思想是局部速度选择和相互避让，而不是互相硬推。
RTS flow-field / corridor 思路: 大群体共享方向场或路径走廊，每个单位只做局部修正。
```

参考链接：

```text
https://docs.godotengine.org/en/stable/getting_started/step_by_step/nodes_and_scenes.html
https://gafferongames.com/post/deterministic_lockstep/
https://www.red3d.com/cwr/steer/gdc99/
https://gamma-web.iacs.umd.edu/ORCA/
```

## 设计总原则

游戏不要围绕“单位类”和“建筑类”扩张。

游戏应该围绕这些稳定概念扩张：

```text
Entity
Component
System
Command
Owner
Spec
View
Event
Metric
```

一句话：

```text
Command 输入意图
Simulation 推进事实
View 表现事实
Metric 判断质量
```

## 99 分验收口径

不是“代码能跑”就算完成。

99 分要满足：

```text
框选 30 个混合单位攻击单个建筑，不挤成一团
远程单位停在射程外，近程单位自然前压
已经开火的单位不会被后排推走
单位到达目标后紧凑但不抖动
狗打狗、猫打猫、狗猫同盟都不混淆颜色和敌我
建筑、炮塔、单位、资源、目标都用同一实体语言
炮塔是实体，炮管是挂点
普通建筑不因为是建筑就拥有防御逻辑
Godot 节点不参与权威玩法判断
同一 seed + command log 可重放并得到相同 state hash
新增单位/建筑主要新增 spec，不需要改十几个系统
沙盒可以快速生成单位、调时间、调阵营、调关系、调环境
每个重要系统都有可视化 debug 和数值指标
```

## 系统分层

### Simulation

只负责真实玩法。

```text
EntityWorld
EntitySpec
EntityInstance
ComponentState
System
Command
Event
StateHash
```

Simulation 不引用 Godot Node。

### Presentation

只负责显示和声音。

```text
EntityView
WorldLayer
HudLayer
MinimapLayer
EffectPool
AudioCue
AnimationState
```

Presentation 可以读 Simulation 快照，但不能直接改 Simulation 状态。

### Authoring

负责写数据。

```text
UnitDesign
BuildingDesign
TurretDesign
ProjectileDesign
ResourceDesign
ObjectiveDesign
FactionRoster
CampaignAvailability
SandboxLoadout
```

Authoring 可以使用浅继承提高书写效率，但运行时不使用继承表达能力。

## Entity 语言

`EntityKind` 只做分类：

```text
Unit
Building
Turret
Resource
Objective
Projectile
Effect
```

能力来自组件。

示例：

```text
步兵
Kind: Unit
Components: Transform, Health, Selectable, Commandable, Vision, Collision, Movement, WeaponUser
```

```text
兵工厂
Kind: Building
Components: Transform, Health, Selectable, Vision, Footprint, ProductionQueue, RallyPoint, Power, BuildRadius
```

```text
防御炮塔
Kind: Turret
Components: Transform, Health, Selectable, Vision, Footprint, WeaponUser, WeaponMountState, Power
```

```text
信号塔目标
Kind: Objective
Components: Transform, Health, Selectable, Vision, Objective, Repairable, SignalNode
```

## Owner / Faction / Relation

运行时实体只保存 `OwnerId`。

玩家状态保存：

```text
OwnerId
FactionId
TeamId
OwnerColor
ControllerKind
```

关系来自：

```text
RelationTable(OwnerId, OwnerId)
```

实体美术只认：

```text
OwnerColor
```

阵营负责形状、单位池、战役可用性、AI 偏好，不负责运行时敌我判断。

## 命令系统

输入必须转成命令。

核心命令：

```text
SelectCommand
MoveCommand
AttackCommand
AttackMoveCommand
StopCommand
HoldPositionCommand
PatrolCommand
GuardCommand
BuildCommand
PlaceBuildingCommand
ProduceCommand
CancelProductionCommand
SetRallyPointCommand
HarvestCommand
RepairCommand
SetStanceCommand
SandboxSpawnCommand
SandboxTimeCommand
```

所有命令进入：

```text
CommandBuffer
```

每个 tick 按稳定顺序处理：

```text
tick
ownerId
commandSequence
entityId list
payload
```

这样可以服务：

```text
重放
联机
回滚测试
AI 调试
玩家操作分析
```

## 群体移动

不要让每个单位独立理解鼠标点。

流程：

```text
1. 玩家右键目标点
2. 生成 GroupMoveCommand
3. 根据选择单位半径/速度/当前相对位置生成目标槽位
4. 共享路径走廊或方向场
5. 每个单位执行局部 steering
6. 进入软到达状态
7. 达到紧凑稳定状态后停止微修正
```

单位移动目标分层：

```text
IntentPoint: 玩家点击点
FormationSlot: 内部分配的落点
PathCorridor: 全局路径结果
SteeringTarget: 当前局部目标
VisualTarget: UI 展示给玩家的命令点
```

玩家不应该看到 FormationSlot 抖动。

## 寻路策略

优先级：

```text
直线可达 -> 直接走
直线被阻挡 -> A* / nav corridor
大群体同目标 -> 共享 corridor 或 flow field
接近目标 -> 局部 steering
局部拥挤 -> 软避让
严重堵塞 -> 有节流地重算路径
```

路径质量指标：

```text
travelInflation: 实际路程 / 直线路程
cornerCount: 转角数量
jitterAfterArrival: 到达后速度/角速度残留
finalCompactness: 最终队形紧凑度
stuckSeconds: 卡住时间
repathCount: 重算次数
```

## 局部避让

局部避让不要把单位变成绕圈机器。

权重顺序：

```text
目标吸引 > 严重碰撞分离 > 侧向避让 > 编队保持 > 轻微邻居分离
```

正在开火、正在修理、正在卸货、正在建造的单位应该有更高站位权重。

其他单位绕开它们。

## 攻击找位

群体攻击不能移动到目标中心。

流程：

```text
1. 目标实体提供攻击包围形状：圆形、矩形、长条、建筑 footprint 边缘
2. 每个攻击单位根据武器射程得到可攻击环/带
3. 系统生成候选攻击点
4. 已在射程内且有瞄准线 -> 直接成为 firing anchor
5. 未在射程内 -> 选择最近可用攻击点
6. 后排单位避开 firing anchor
7. 目标死亡后按命令语义转火或停下
```

候选点评分：

```text
距离短
路径直
不挡住友军
能开火
不进入最小射程
不进入危险区域
不推开 firing anchor
保留队伍大致朝向
```

## 战斗系统

武器不是只有 DPS。

武器需要：

```text
range
minimumRange
targetProfile
damageProfile
cooldown
warmup
burst
reload
projectileKind
speed
spread
arc
requiresFacing
turnRate
fireWhileMoving
lineOfSightRule
impactEffect
soundCue
```

武器状态机：

```text
Idle
Acquire
Rotate
Warmup
Fire
Cooldown
Reload
Reacquire
```

炮塔挂点：

```text
BodyFixed
Independent
Omni
```

`Independent` 挂点有自己的角度和转速。

## Projectile / Effect

不是所有特效都是实体。

规则投射物是实体：

```text
导弹
可拦截炮弹
追踪弹
地雷
持续伤害区域
```

纯表现不是实体：

```text
枪口闪光
短火花
尘土
装饰性爆炸
曳光线
```

表现对象用池化。

## 建造系统

建造必须支持三种体验：

```text
C&C 式：先在侧栏建造，完成后放置
即时放置式：点击后直接放下并开始施工
任务脚本式：修复/重启/占领已有目标
```

统一数据：

```text
BuildSpec
- outputEntitySpecId
- category
- cost
- buildTime
- footprint
- requiredTech
- requiredProducer
- requiredPower
- buildRadius
- placementRules
- refund
```

建筑运行组件：

```text
ConstructionState
PowerState
ProductionQueueState
RallyPointState
DockState
BuildRadiusState
```

## 生产系统

生产是建筑能力，不是 UI 特例。

生产队列属于具体生产实体：

```text
ProducerEntityId
Queue
Progress
RallyPoint
PausedReason
CancelRefund
```

UI 可以聚合：

```text
所有兵营
所有工厂
当前选中工厂
全局生产状态
```

但权威队列仍属于具体实体。

## 经济系统

经济要从一开始支持可调节设计。

资源节点：

```text
ResourceNode
- amount
- gatherRateModifier
- depletionBehavior
- visibilityRule
- corruptionState
```

采集单位：

```text
ResourceCargo
HarvesterBehavior
DockReservation
DeliveryTarget
```

码头需要预约，避免多个采矿车挤同一个卸货点。

## 视野和迷雾

玩法可见性和视觉雾要分开。

玩法层：

```text
Visible
Explored
Unexplored
LastKnownStatic
LastKnownEnemyTrail
```

表现层：

```text
mask texture
soft edge
memory tone
corruption tone
night tone
```

攻击目标进入迷雾时：

```text
短时间保留 last-known-position
直接射击是否继续由武器规则决定
导弹可选择失锁/追踪到最后位置/继续追踪
```

## AI

AI 不应该直接作弊调用内部状态。

AI 也提交命令：

```text
BuildCommand
ProduceCommand
AttackMoveCommand
DefendCommand
RepairCommand
ScoutCommand
```

AI 分层：

```text
EconomyPlanner
ProductionPlanner
DefensePlanner
AttackWavePlanner
ScoutPlanner
TacticalMicro
```

战役 AI 可以有脚本权重，但仍尽量走命令系统。

## 战役系统

战役不要直接写死单位行为。

战役驱动：

```text
Objective graph
Trigger
Condition
Action
Dialogue cue
EnvironmentTone cue
AI director cue
Unlock cue
```

狗狗战役里的“修路灯、守火炮、恢复信号塔”都应是实体/组件任务：

```text
Repairable Objective
SignalNode
DefenseTimer
T4 Turret Entity
EnvironmentTone Transition
```

## 美术和 UI

单位、建筑、炮塔共享：

```text
ArtRecipe
ArtLayer
ArtBinding
ColorRole
EnvironmentResponse
```

颜色原则：

```text
Body: 低疲劳主体
Ink: 线条和轮廓
Owner: 所有权贴纸
Effect: 技能/状态
Warning: 危险
Shadow: 接地/遮蔽
```

关系颜色只出现在：

```text
选择框
血条边
目标框
命令线
小地图
警告
```

不要让实体本体同时承担阵营色、玩家色、敌我色。

## 沙盒

开发沙盒必须比普通关卡更强。

需要：

```text
生成任意 entity spec
切换 owner/faction/team/relation
切换 day/fog/night/corruption
调整时间推进倍率
显示路径/槽位/避让/debug
显示攻击环和 firing anchor
显示组件状态
显示 command log
显示 state hash
一键生成 30 单位群攻测试
一键生成采矿拥堵测试
一键生成炮塔防守测试
```

## Debug 指标

每个复杂系统都要有指标。

移动：

```text
pathInflation
cornerCount
arrivalJitter
compactness
stuckSeconds
repathCount
avoidanceImpulse
```

战斗：

```text
timeToFirstShot
targetSwitchCount
overkillDamage
shotsBlockedByFacing
rangeSlotFailures
anchorPushEvents
```

经济：

```text
harvesterIdleTime
dockWaitTime
resourceTripTime
creditsPerMinute
refineryCongestion
```

AI：

```text
attackWaveInterval
unitComposition
producerIdleTime
resourceFloat
baseDefenseCoverage
```

性能：

```text
simulationTickMs
renderMs
entityCount
projectileCount
effectPoolUsage
fogUpdateMs
pathRequestsPerSecond
```

## 实施顺序

### Phase 1: Entity Skeleton

```text
EntityKind
EntityId
OwnerId
EntitySpec
EntityInstance
ComponentState
EntityWorld
EntityCommand
StateHash
```

验收：不破坏现有游戏，现有 UnitSpec 可映射到 EntitySpec。

### Phase 2: Unit Entity Conversion

```text
UnitDesign -> UnitSpec -> EntitySpec
UnitInstance -> Entity component state
UnitBattlefield -> EntityWorld facade
```

验收：狗狗/猫猫单位仍能在沙盒和普通战斗中生成、移动、攻击、采矿。

### Phase 3: Building Entity Conversion

```text
BuildingDefinition + BuildDefinition -> Building/Construction EntitySpec
UnitBattlefieldBuildingTarget -> EntityInstance + components
ProductionQueue -> component
Dock/Rally/Power -> component
```

验收：建筑生产、采矿卸货、炮塔攻击、HQ 胜负都走实体路径。

### Phase 4: Command Lock

```text
CommandBuffer
GroupCommand
FormationAssignment
AttackSlotAssignment
CommandLog
ReplayHarness
```

验收：30 单位群体移动和群体攻击稳定。

### Phase 5: Visual Lock

```text
ArtRecipe
ColorRole
OwnerColor
EnvironmentTone
EntityView
EffectPool
```

验收：狗打狗、猫打猫、白天/夜晚/雾/腐化都清晰。

### Phase 6: AI / Campaign / Sandbox Lock

```text
AI command planner
Objective graph
Scenario triggers
Sandbox stress tools
Debug metrics
```

验收：战役目标、AI、沙盒压力测试都不绕开实体/命令系统。

## 不做什么

不要为了短期方便继续扩展：

```text
UnitInstance 大对象
UnitBattlefieldBuildingTarget 第二套建筑运行时
建筑自带防御逻辑
Faction 参与运行时敌我判断
关系色进入实体本体美术
Godot Node 反向驱动 gameplay
每单位独立 A* 解决群体移动
纯视觉特效进入完整实体系统
```

这些都会让项目后期变得难以扩展。
