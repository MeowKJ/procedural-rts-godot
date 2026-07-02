# Review Record - M9 Ability Cooldown Buffers

Step:
M9 AbilitySystem cooldown buffer reuse (#79)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/sim/systems/AbilitySystem.cs`, `scripts/core/sim/systems/ability/AbilitySystem.Ticking.cs`, `scripts/core/sim/systems/ability/AbilitySystem.TargetingCosts.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 ability 平衡、冷却时长、charge 规则、命令语义、UI 或新 ability 内容。

Implementation summary:
- 在 `AbilitySystem` 增加 `_cooldownScratch`，让 cooldown tick 与 `SetCooldown(...)` 复用同一个 caller-owned buffer。
- 将 `TickCooldowns(...)`、`ApplyAbility(...)`、`SetCooldown(...)` 调整为实例路径，以便复用 scratch buffer。
- 用显式循环替代 cooldown 查询里的 `runtime.Cooldowns.Any(...)`。
- 扩展 `ReviewGate simhot`，禁止 AbilitySystem cooldown 路径回退到 `runtime.Cooldowns.ToArray()`、`Append(...).ToArray()` 或 cooldown `Any(...)`。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED; ability scenarios including repair-field, shield-field, scan, deploy, ability-legality, and targeted-repair stayed deterministic.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings, including AbilitySystem cooldown buffer evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after budget evidence updates.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including PerfSmoke and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Simulation allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: `AbilityRuntimeComponentState` still writes immutable cooldown snapshots, so component writeback still allocates the final array when cooldown state changes.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open for broader profiler-guided cleanup.
- Items left open: construction/placement edge allocations, immutable path/queue snapshots, and future profiler-guided GC work.
- Reason: This closes only the AbilitySystem cooldown allocation child slice.
