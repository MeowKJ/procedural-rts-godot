# Review Record - M9 UnitBattlefield Production Option Buffers

Step: #173 `[M9] Reuse UnitBattlefield production option state buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate simhot / DesktopHudQa / AiOpponentLoopQa
Integrator AI: Remote Linux Codex

Scope:
- Add reusable `UnitBattlefield` result buffers for legacy-kind and UnitDesign production option states.
- Change `ProductionOptionStates(...)` and `ProductionDesignOptionStates(...)` to clear, fill, sort, and return those buffers instead of allocating new lists per refresh.
- Extend `UnitBattlefieldProductionAllocationReviewGate` so `ReviewGate simhot` forbids the state-list allocation from returning.
- Non-goals: production balance, HUD layout, enemy production AI policy, producer selection, queue/cancel behavior, or legacy `GameState` production options.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-unitbattlefield-production-option-buffers`
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
- Returned lists are still mutable internally by `UnitBattlefield`; current HUD, QA, and AI consumers read or copy them synchronously.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #173 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
