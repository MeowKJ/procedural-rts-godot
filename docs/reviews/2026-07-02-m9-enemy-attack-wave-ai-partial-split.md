# Review Record - M9 Enemy Attack Wave AI Partial Split

Step:
M9 enemy attack wave AI partial split (#100)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.*.cs`, `tools/ReviewGateDomains/EnemyAttackWaveAiReviewGate.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: 不修改 wave timing、defense timing、scout、target priority、command semantics、AI difficulty、balance、UI 或视觉表现；本切片不清理 LINQ allocation。

Implementation summary:
- 将 `UnitBattlefieldEnemyAttackWaveAi` 改为 partial class，主文件只保留 public API、timers、`Update(...)` 和 defense-order flow。
- 新增 `UnitBattlefieldEnemyAttackWaveAi.UnitSelection.cs`、`Targeting.cs`、`Geometry.cs`，分别承载 unit selection、target/scout selection 和几何/center helper。
- 新增 `EnemyAttackWaveAiReviewGate` 并接入 `ReviewGate simhot`，要求四个 enemy attack-wave AI partial 文件存在且各自不超过 200 行。
- 同步 `TODO.md` 和 file-size discipline review record 中的 validation-tool suite budget evidence：Validation tool suites current source budget: 138 C# source files / 18706 total lines across 54 suites; largest C# file tools/CombatBehaviorSkirmish/SkirmishAi.cs has 393 lines; largest suite tools/ReviewGateDomains has 1000 lines.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass
  Evidence: runtime loop passed with 6 waves, 6 wave bridge commands, 1200 HQ damage, construction command probe hash `8C1715CDCC58D60A`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings after the partial split evidence was added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: ReviewGate filesize passed with 0 errors and 0 warnings after syncing validation-tool budget evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including SimReplay, CombatBehavior, AiOpponentLoopQa, ReviewGate, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: 文件结构治理切片；没有 rendering、HUD、theme 或 gameplay visual 变更。

Reviewer result:
- Status: pass.
- Required fixes: 已将 ReviewGate partial-split evidence 移到独立 `EnemyAttackWaveAiReviewGate`，避免 `RegressionReviewGate.cs` 超过 200 行；已同步 exact validation-tool source-budget evidence。
- Residual risks: 本切片只做 mechanical split；`UnitBattlefieldEnemyAttackWaveAi` 内的 wave/defense/target LINQ allocation debt 仍保留给后续小 Issue。

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: enemy attack-wave AI allocation cleanup, broader profiler-guided GC cleanup under #10.
- Reason: #100 只关闭文件大小治理 prerequisite，后续 allocation paydown 可以在 partial 文件中小步完成。
