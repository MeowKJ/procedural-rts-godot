# Player Control Architecture

状态：M8 统一玩家控制架构锁定稿。

本 ADR 定义玩家身份、控制来源、AI/模型、观测、命令入口和模拟裁判的边界。当前 live runtime 已收敛到 `UnitBattlefield` 持有的单一 `EntityWorld`；后续控制器、AI、回放和外部模型接入必须保持这条边界。

## 核心原则

`Faction` 是内容，不是玩家身份。它决定可用单位、建筑、图标和文案。

`PlayerSlot` 是比赛中的玩家席位。它绑定 `OwnerId`、队伍、阵营选择、颜色、连接状态和胜负状态。

`PlayerController` 是输入来源。人类本地输入、脚本 Bot、回放、远程客户端、LLM 测试器和 RL 策略都只是 controller。

`PlayerAgent` 是 AI/模型的思考方式。它只读 `ObservationView`，输出 `PlayerCommand[]`，不拥有权威状态。

`Transport` 是网络或进程通道。它只搬运观测和命令，不裁判合法性。

`SimulationAuthority` 是裁判。它拥有固定 tick、命令排序、合法性验证和 `EntityWorld` 推进权。

`CommandSystem` 是模拟命令的唯一入口。任何 controller 或 agent 都不能直接写 `EntityWorld`、单位状态、建筑状态、资源、目标或冷却。

## 最小对象边界

```text
PlayerSlot
- SlotId
- OwnerId
- FactionId
- TeamId
- OwnerColor
- ControllerId
- ConnectionState
- OutcomeState
```

`PlayerSlot` 是比赛配置和运行时身份，不是输入设备。

```text
PlayerController
- ControllerId
- ControllerKind
- ControlledSlotIds
- Poll(ControllerContext) -> PlayerCommand[]
```

`PlayerController` 可以是 Godot 节点、headless QA 适配器、回放驱动器或远程客户端适配器。它只提交意图。

```text
PlayerAgent
- AgentId
- AgentKind
- Think(ObservationView) -> PlayerCommand[]
```

`PlayerAgent` 可同步或异步，但返回结果必须经过 controller 和 gateway。外部模型超时、失败或越权时只能丢弃命令，不能暂停权威模拟。

```text
ObservationView
- ViewerSlotId
- Tick
- VisibleEntities
- OwnEntities
- KnownPlayers
- Resources
- BuildAndProductionAffordances
- CommandFeedback
```

`ObservationView` 是只读快照。它只能包含该玩家可见或已探索允许展示的信息，不能泄露雾外敌人状态、隐藏队列、敌方内部目标、RNG、未来命令或调试真值。

```text
PlayerCommand
- IssuerSlotId
- ClientSequence
- TargetTick
- Kind
- Payload
```

`PlayerCommand` 是玩家意图协议，不是模拟状态修改。选择、移动、攻击、建造、生产、技能、编队、取消、集结点和沙盒开发命令都走同一协议族。

```text
CommandGateway
- Authenticates controller -> slot rights
- Orders commands by tick / sequence
- Validates ownership, visibility and payload shape
- Converts accepted commands into EntityCommandBuffer entries
- Emits rejected command feedback
```

`CommandGateway` 只做入口验证和协议转换。具体系统合法性仍由 `CommandSystem`、`ConstructionSystem`、`ProductionSystem`、`CombatSystem` 等权威系统二次检查。

```text
SimulationAuthority
- Owns SimClock and EntityWorld
- Drains accepted commands at fixed tick boundaries
- Runs system pipeline in deterministic order
- Produces projections, events and state hashes
```

`BattleRoot` 是 live Godot 编排者，只提交 frame delta、玩家意图并消费投影/事件。`UnitBattlefield` 持有唯一 `EntityWorld` 和固定时钟，负责在 tick 边界排空 gateway 命令并推进系统管线。`WorldPresentationEnvironment` 只持有雾、主题和信号节点等表现服务，不保存模拟状态。

## 标准数据流

单机人类玩家：

```text
Godot input
-> HumanPlayerController
-> PlayerCommand[]
-> CommandGateway
-> EntityCommandBuffer
-> CommandSystem
-> Simulation Tick
-> EntityProjection / SimEvent
-> Presentation
```

单机敌人 AI：

```text
SimulationAuthority builds ObservationView(PlayerSlotId.Two)
-> ScriptedBotController
-> Utility/Scripted PlayerAgent
-> PlayerCommand[]
-> CommandGateway
-> same CommandSystem path as human commands
```

本地多人：

```text
Keyboard/controller A -> LocalHumanController(slot 1)
Keyboard/controller B -> LocalHumanController(slot 2)
Both command streams -> one local SimulationAuthority
```

云联机：

```text
Client input -> NetworkTransport -> Server CommandGateway
Server SimulationAuthority -> authoritative tick stream/projections
Client presentation -> prediction/reconciliation later, never authority
```

回放：

```text
Recorded command log -> ReplayController
-> CommandGateway
-> deterministic SimulationAuthority
-> state hash comparison
```

AI QA / RL / LLM 测试：

```text
ObservationView(slot)
-> Agent sandbox adapter
-> PlayerCommand[]
-> timeout/rate/schema checks
-> CommandGateway
-> Simulation Tick
```

玩家自带 LLM 或外部 AI：

```text
ObservationView(redacted)
-> ExternalAgentController sidecar
-> strict command schema
-> timeout + rate limit + room policy
-> CommandGateway
```

默认策略：竞技或标准匹配禁用外部模型；自定义房间或测试模式可启用。外部模型永远不能获得隐藏信息，不能直接改状态，不能绕过限频和合法命令校验。

## 合法性分层

Gateway 层必须拒绝：

- controller 未绑定该 `PlayerSlot`。
- `IssuerSlotId` 与 controller 权限不匹配。
- `ClientSequence` 对同一 controller 回退或重复。
- payload 类型错误、数量过大或包含非法 id。
- 命令引用非己方实体但该命令需要己方 ownership。
- 命令引用雾外敌方实体且当前命令需要可见目标。
- 建造/生产/技能请求引用该 slot 未解锁或不可用的 spec。
- 沙盒开发命令出现在非 sandbox authority。

系统层继续验证：

- 移动、攻击、建造、生产、采矿、修理、技能等实际规则。
- 资源、冷却、范围、碰撞、footprint、tech、power、visibility。
- 同 tick 冲突和 deterministic ordering。

Presentation 层只接收：

- `EntityProjection`
- `ObservationView` 的展示子集
- `SimEvent`
- rejected command feedback

Presentation 层不能读写 authoritative component state。

## 与现有代码的落地关系

已落地并应复用：

- `PlayerSlotId` 表示 live runtime 的玩家席位。
- `OwnerId` 表示 sim runtime ownership。
- `OwnerRelationTable` 决定敌我关系。
- `EntityCommandBuffer` / `CommandSystem` 是 sim 命令入口。
- `VisibilityIndex` / `VisionSystem` 是隐藏信息边界的基础。
- `CommandGateway` 是所有 live 玩家和 AI 命令的协议入口。
- `SelectionController`、`BuildPlacementController`、`ProductionController` 和 scripted AI controllers 只表达输入来源，不拥有权威状态。
- 普通遭遇战与 authored map 都通过 `MapSpec -> MapLoader.Load -> UnitBattlefield.AdoptLoadedMap` 建立同一条运行链。

后续扩展顺序：

1. 扩展 `ObservationView` 时先保持可见性裁剪和只读快照边界。
2. 新 controller / agent 只产生 `PlayerCommand`，不得获得 world 写权限。
3. replay、network 和 external agent transport 复用同一 gateway schema。
4. prediction / reconciliation 只能叠加在命令和投影边界上，不能创建第二权威 world。

## 非目标

- 不在本 ADR 实现网络同步、预测或回滚。
- 不接入真实 LLM API。
- 不训练 RL 模型。
- 不改变战斗平衡、阵营内容、剧情或 UI 视觉。
- 不把 `UnitBattlefield` 变成第二份模拟状态容器。
- 不允许任何 controller、agent 或 transport 直接写权威状态。

## 后续实现 Issue

本 ADR 对应的第一批小实现切片：

- #106 `[M8] Add minimal PlayerController contract`
- #107 `[M8] Add ObservationView read-only snapshot`
- #108 `[M8] Add CommandGateway validation shell`

这些 Issue 都必须保持小范围、可验证，并在实现前后用 ReviewGate 和 headless gates 锁定边界。
