# Review Record - M9 Auto-Acquire Target Scan

Step:
M9 auto-acquire target scan (#97)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/battlefield/UnitBattlefield.VisibilityCombat.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no weapon priority, target legality, damage, stance, AI, balance, UI, visual, or EntityWorld `CombatSystem` target-search changes.

Implementation summary:
- Replaced `UnitBattlefield.AcquireAutoTarget(...)` anonymous ordered LINQ candidate query with a single explicit scan.
- Preserved existing filters for current targets, ignore-move, non-idle harvesters, relation legality, weapon target legality, sight range, positive priority, and nearest target at equal priority.
- Added `ReviewGate simhot` evidence so the old anonymous candidate and ordered LINQ chain cannot return.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded; one transient MSB3026 copy retry warning resolved during the same build.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including weapon hit rules, shared threat propagation, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after review-record format and exact budget evidence fixes.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Runtime allocation refactor only; no rendering or UI changed.

Reviewer result:
- Status: pass after required format/budget fixes.
- Required fixes: rewrote the review record to the standard fields and synced exact validation-suite source-budget evidence.
- Residual risks: static `ReviewGate` checks are string-based; broader M9 allocation debt remains open under #10.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: broader profiler-guided UnitBattlefield and projection allocation cleanup.
- Reason: This closes only the auto-acquire target scan allocation child slice.
