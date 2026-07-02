# Review Record - M9 Building-Target Combat Event Buffers

Step:
M9 building-target combat event buffer reuse (#98)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.BuildingTargetCombatBridge.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.VisibilityCombat.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no building-target damage, projectile stepping, target selection, death/removal, AI, balance, UI, visual, or combat-system convergence changes.

Implementation summary:
- Added reusable damaged, destroyed, and dead building id buffers for building-target combat event application.
- Replaced the building-target bridge work check and combat state sync LINQ scans with explicit loops.
- Replaced dead building id `Select/Where/Concat/Distinct/ToList` materialization with a reusable buffer collector.
- Moved the building-target combat bridge into a focused partial file so `UnitBattlefield.VisibilityCombat.cs` stays under the yellow file-size threshold.
- Added `ReviewGate simhot` evidence so the old local `HashSet` and dead-id LINQ paths cannot return.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded; one transient MSB3026 copy retry warning resolved during the same build. VerifyAll build later passed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including weapon hit rules, shared threat propagation, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after syncing exact validation-suite budget evidence.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Runtime allocation refactor only; no rendering or UI changed.

Reviewer result:
- Status: pass.
- Required fixes: split `UnitBattlefield.BuildingTargetCombatBridge.cs` out of `VisibilityCombat.cs` to keep file-size governance at 0 warnings.
- Residual risks: static `ReviewGate` checks are string-based; `CombatBehavior` and `VerifyAll` cover runtime behavior. Broader M9 allocation debt remains open under #10.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: broader profiler-guided UnitBattlefield and projection allocation cleanup.
- Reason: This closes only the building-target combat event buffer child slice.
