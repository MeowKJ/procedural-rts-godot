# Docs Index

这个目录保存架构、协作、设计和历史审查资料。它不是当前任务进度的唯一来源；当前执行状态优先看仓库根目录的 `TODO.md`、ReviewGate 输出和最新 review record。

## 当前优先阅读

- `EntityFrameworkArchitecture.md`：实体框架的长期目标和边界，单位、建筑、炮塔、资源、目标等都应收敛到同一套实体语言。
- `RTS99Design.md`：99 分 RTS 目标设计，包含命令、移动、战斗、建造、生产、AI、UI 和沙盒方向。
- `FileStructureGovernance.md`：文件拆分、目录治理、文件大小红线、Bridge/Legacy 删除条件和 ReviewGate 应强制的结构纪律。
- `AICollaborationProtocol.md`：多 AI 协作、review record 和验收节奏。

## 专项设计

- `FogOfWarDesign.md`：战争迷雾的数据、渲染、性能和 Godot 实现计划。
- `GitConventions.md`：commit message 和提交粒度规范。
- `unit-data/`：单位纸面资料，只作为设计输入，不直接驱动运行时代码。新增单位最终应沉淀为代码里的 spec，而不是停留在文档。

## 历史和证据

- `reviews/`：每个已实现 TODO slice 的 durable evidence。这里是审查证据库，不是日常导航入口。
- `TODO-Archive-2026-06-29.md`：历史 TODO 快照，只用于追溯背景，不代表当前待办。
- `AI_B_COMPLETION_NOTE.md`：历史交接说明，只用于追溯，不代表当前完成状态。
- `Refactor99Plan.md`：早期大重构计划和阶段记录。当前执行口径以 `EntityFrameworkArchitecture.md`、`RTS99Design.md`、`FileStructureGovernance.md` 和根目录 `TODO.md` 为准。

## 维护规则

- 新的长期规则写进对应专题文档，不写进 review record。
- 新的实现证据写进 `reviews/`，不要把 review record 当作架构文档维护。
- 已过期但可能有追溯价值的资料保留原文件，并在本索引标记为历史；不要在没有迁移说明的情况下直接删除。
- 如果某个文档只剩历史价值，在文件顶部加一句 `Status: Historical reference`，并在本索引登记。
- 文档新增后必须能被本索引解释用途，否则默认是文档债务。
