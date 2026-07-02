# Review Record - M9 BattleRoot Uniform Stance

Step:
M9 BattleRoot uniform stance LINQ cleanup (#130) - m9-battleroot-uniform-stance

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- 文件/系统：`scripts/BattleRoot.Selection.cs`、`tools/ReviewGateRuntime/BattleRootHudAllocationReviewGate.cs`。
- 非目标：不改变 stance command、command-card 状态、HUD 文案或 selection 语义。

实现摘要:
- 将 `SelectedUniformStance(IReadOnlyList<UnitModel>)` 与 `SelectedUniformStance(IReadOnlyList<UnitInstance>)` 的 LINQ `All(...)` 改为从 index 1 开始的显式扫描。
- 扩展 `BattleRootHudAllocationReviewGate`，要求 uniform stance 使用显式 indexed scan，并禁止 `.All(unit => unit.Stance == stance)` 回归。

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
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=m9-battleroot-uniform-stance`
  Result: pass
  Evidence: ReviewGate review record check passed with 0 errors and 0 warnings.
- Command: `GODOT_BIN=$(command -v godot-dotnet) DOTNET_ROLL_FORWARD=Major sh tools/verify-all.sh`
  Result: pass
  Evidence: VerifyAll passed 23/23 steps, including ReviewGate, PerfSmoke, and Godot headless QA.

人工/视觉验证:
- 结果：不适用。
- 证据：只替换等价的 stance uniformity 判断；空列表、多 stance 列表和单一 stance 列表语义保持不变。

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: ReviewGate 是静态防回归检查，最终关闭 issue 前仍需要集中运行 build、窄门、ReviewGate 和 VerifyAll。

TODO update:
- Items marked done: none.
- Items left open: #10 / #58 的 broader M9 allocation paydown 继续打开，后续按 profiler-guided 小 slice 处理。
