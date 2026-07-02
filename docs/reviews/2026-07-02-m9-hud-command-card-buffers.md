# Review Record - M9 HUD command-card buffers

Step: #128 Reuse HUD command-card state buffers
Milestone: M9 Elegance & Decoupling / presentation allocation paydown
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation
Integrator AI: Remote Linux Codex

Scope:
- 文件：`scripts/ui/hud/HudLayer.State.cs`、`tools/ReviewGateRuntime/BattleRootHudAllocationReviewGate.cs`、`TODO.md`。
- 目标：`HudLayer.SetCommandCardState(...)` 不再为每次 refresh 分配 ordered state array、active id set、stale key array。
- 非目标：不修改 HUD visual style、button layout、12-slot cap、hotkeys、disabled reason、production rules 或 broad UI design。

Implementation summary:
- 增加 `_commandCardStates`、`_commandCardActiveIds`、`_commandCardStaleIds` reusable buffers。
- 用显式 first-12 copy 保持旧 button order，用 stale-key buffer 分两步移除按钮，避免枚举期间修改 dictionary。
- `ReviewGate presentation` 现在要求 command-card buffers 存在，并禁止旧 `Take().ToArray()` / `Select().ToHashSet()` / stale-key `ToArray()` pattern 回归。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass，1280x720、1600x900、1920x1080、high-DPI layout constraints 和 HUD UiFactory extraction 通过。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- all --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass，23/23 steps 通过，包含 PerfSmoke、DesktopHudQa、ActiveBattlePerfQa 和 Godot headless QA。

Reviewer result:
- Status: pass。
- Required fixes: none。
- Residual risks: ReviewGate 是静态文本门禁，运行时 allocation magnitude 继续由 PerfSmoke / ActiveBattlePerfQa / profiler-guided cleanup 跟踪。

TODO update:
- 已在 M9 per-tick allocation paydown 父项记录 #128 follow-up。
- 父项 #10 保持 open。
