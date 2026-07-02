# Review Record - M9 Enemy Attack Wave Unit Buffers

Step:
M9 enemy attack wave unit buffer reuse (#101)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.UnitSelection.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.Targeting.cs`, `tools/ReviewGateRuntime/EnemyAttackWaveAiReviewGate.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: 不修改 target selection、visible target priority、aggression radius、wave/defense timing、AI difficulty、balance、UI、presentation 或 combat behavior。

Implementation summary:
- 为 `UnitBattlefieldEnemyAttackWaveAi` 增加 `_waveCandidateUnits`、`_waveUnits`、`_waveUnitIds`、`_defenseUnits`、`_defenseUnitIds` 和复用的 `UnitDistanceComparer`。
- 将 wave unit selection 改为 `CollectAvailableWaveUnits(...)`：显式扫描可用 combat units，按 base distance/id 原地排序，跳过 reserve，再按 X/id 原地排序并 trim 到 maximum wave size。
- 将 defense unit selection 改为 `CollectAvailableDefenseUnits(...)`：显式扫描 base/target 半径内可用 combat units，按 target distance/id 原地排序并 trim 到 6 个 defender。
- 将 wave attack、defense attack、scout move 的 command id 收集改为 `CollectUnitIds(...)` 复用 list；wave command pulse 更新改为显式循环。
- 将 `EnemyAttackWaveAiReviewGate` 移到 `tools/ReviewGateRuntime` 并扩展 `ReviewGate simhot` evidence，避免 `tools/ReviewGateDomains` 超过 1000-line suite budget。
- 同步 exact validation-tool source-budget evidence：Validation tool suites current source budget: 138 C# source files / 18742 total lines across 54 suites; largest C# file tools/CombatBehaviorSkirmish/SkirmishAi.cs has 393 lines; largest suite tools/ReviewGateDomains has 954 lines.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate project build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass
  Evidence: runtime loop passed with 6 waves, 6 wave bridge commands, 1200 HQ damage, and construction command probe hash `8C1715CDCC58D60A`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings after unit-buffer allocation evidence was added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: ReviewGate filesize passed with 0 errors and 0 warnings after syncing validation-tool budget evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including SimReplay, CombatBehavior, AiDifficultySmoke, AiOpponentLoopQa, ReviewGate, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: runtime AI allocation refactor only; no rendering, HUD, art, or theme changes.

Reviewer result:
- Status: pass.
- Required fixes: tightened the ReviewGate forbid pattern so it checks only the old allocating call sites and does not match the new `CollectAvailableWaveUnits(...)` helper; reran simhot/full ReviewGate successfully.
- Residual risks: target selection and center calculation still use LINQ materialization and remain scoped to follow-up slices; this issue only covers unit/id buffer reuse.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: enemy attack-wave target scan and center calculation allocation cleanup, plus broader profiler-guided GC cleanup under #10.
- Reason: #101 closes only wave/defense unit and id buffer reuse.
