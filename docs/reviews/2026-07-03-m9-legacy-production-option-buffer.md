# Review Record - M9 Legacy Production Option Buffer

Step: #172 `[M9] Reuse legacy production option state buffer`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / DesktopHudQa / CombatBehavior
Integrator AI: Remote Linux Codex

Scope:
- Add reusable `GameState` storage for legacy production option result states.
- Change `ProductionOptionStates(...)` to clear, fill, and return that reusable storage instead of allocating a new list per refresh.
- Extend `GameStateAllocationReviewGate` so `ReviewGate regression` forbids the state-list allocation from returning.
- Non-goals: production balance, HUD layout, producer selection, queue/cancel behavior, or runtime `UnitBattlefield` production options.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-legacy-production-option-buffer`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
- Command: `git diff --check`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- The returned list remains mutable internally by `GameState`; current consumers copy or read it synchronously, matching existing reusable snapshot patterns.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #172 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
