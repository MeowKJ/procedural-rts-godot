# Review Record - M9 Enemy Attack Wave Target Scans

Step:
M9 enemy attack wave target scan reuse (#102)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.Targeting.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.TargetScans.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.Geometry.cs`, `tools/ReviewGateRuntime/EnemyAttackWaveAiReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: 不修改 `UnitBattlefield.BuildingSnapshots()` API，不改变 target priority、visibility、aggression radius、defense radius、AI difficulty、balance、UI、presentation 或 combat behavior。

Implementation summary:
- `TryFindTarget(...)` 改为显式 HQ scan、nearest visible attackable building scan、nearest visible attackable unit scan，保留 HQ-first、building-before-unit 和 first nearest target 语义。
- `TryFindDefenseTarget(...)` 改为显式 nearest visible defense threat scan，保留 distance + id tie-break。
- 新增 `UnitBattlefieldEnemyAttackWaveAi.TargetScans.cs` 承载 target scan helpers，使 `Targeting.cs` 和新 helper 文件都保持在 200 行以内。
- `IsNearOwnedBuilding(...)`、`EnemyBaseCenter(...)`、`EnemyCenter(...)` 改为显式循环与 sum/count，不再使用 `Where` / `Any` / `Select(...).ToList()` / `Aggregate(...)`。
- 扩展 `EnemyAttackWaveAiReviewGate` 的 `ReviewGate simhot` evidence，锁定 target/center scan 不回退到 LINQ ordering/materialization。
- 同步 exact validation-tool source-budget evidence：Validation tool suites current source budget: 138 C# source files / 18776 total lines across 54 suites; largest C# file tools/CombatBehaviorSkirmish/SkirmishAi.cs has 393 lines; largest suite tools/ReviewGateDomains has 954 lines.

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
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings after target/center scan evidence was added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: ReviewGate filesize passed with 0 errors and 0 warnings after syncing validation-tool budget evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including SimReplay, CombatBehavior, AiDifficultySmoke, AiOpponentLoopQa, ReviewGate, PerfSmoke, and Godot headless QA. `godot-active-battle-perf-qa` emitted a non-failing 2 ObjectDB leaked instances warning.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: runtime AI scan refactor only; no rendering, HUD, art, or theme changes.

Reviewer result:
- Status: pass.
- Required fixes: split target scan helpers into `TargetScans.cs` after the first explicit-loop edit pushed `Targeting.cs` over 200 lines.
- Residual risks: `UnitBattlefield.BuildingSnapshots()` still returns an allocated snapshot array; that broader API-level allocation cleanup is intentionally left out of this target-scan slice.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: potential caller-owned building snapshot APIs and broader profiler-guided GC cleanup under #10.
- Reason: #102 closes only enemy attack-wave target/center scan LINQ cleanup.
