# Review Record - M9 BattleRoot Alert Selection LINQ

Step:
M9 BattleRoot alert selection LINQ cleanup (#131) - m9-battleroot-alert-selection-linq

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- 文件/系统：`scripts/BattleRoot.Alerts.cs`、`tools/ReviewGateRuntime/BattleRootHudAllocationReviewGate.cs`。
- 非目标：不改变 alert 文案、冷却、power 规则、UnitBattlefield runtime path 或 HUD 视觉。

实现摘要:
- 将 legacy command preview 的 selected-building check 改为 `HasSelectedLegacyBuildings()` 显式扫描。
- 将 legacy power alert 的 player power-plant check 改为 `HasLegacyPlayerPowerPlant()` 显式扫描。
- 扩展 `BattleRootHudAllocationReviewGate`，禁止 `_state.SelectedBuildings().Any()` 与 power building `Where(...).Any(...)` 回归。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj`
  Result: pass
  Evidence: Selection stress passed 100 cases.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj`
  Result: pass
  Evidence: Desktop HUD QA passed 1280x720, 1600x900, 1920x1080, high-DPI layout constraints, and HUD UiFactory extraction.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation`
  Result: pass
  Evidence: ReviewGate presentation passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=m9-battleroot-alert-selection-linq`
  Result: pass
  Evidence: ReviewGate review record check passed with 0 errors and 0 warnings.
- Command: `GODOT_BIN=$(command -v godot-dotnet) DOTNET_ROLL_FORWARD=Major sh tools/verify-all.sh`
  Result: pass
  Evidence: VerifyAll passed 23/23 steps, including ReviewGate, PerfSmoke, and Godot headless QA.

人工/视觉验证:
- 结果：不适用。
- 证据：只替换 legacy fallback 的 predicate evaluation，返回条件与原 LINQ 表达式一致。

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: ReviewGate 是静态防回归检查，最终关闭 issue 前仍需要集中运行 build、窄门、ReviewGate 和 VerifyAll。

TODO update:
- Items marked done: none.
- Items left open: #10 / #58 的 broader M9 allocation paydown 继续打开，后续按 profiler-guided 小 slice 处理。
