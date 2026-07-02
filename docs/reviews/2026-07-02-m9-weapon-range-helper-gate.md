# Review Record - M9 weapon range helper gate

Step: Lock shared weapon range math against system helper rollback
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate architecture / SimReplay
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/sim/weapon/WeaponMath.cs`, `scripts/core/sim/systems/BuildingTargetCombatSystem.cs`, `scripts/core/sim/systems/TurretCombatSystem.cs`, `scripts/core/sim/systems/combat/CombatEngagementSystem.cs`, `scripts/core/sim/systems/command/CommandSystem.CombatOrders.cs`, `tools/ReviewGateDomains/ArchitectureReviewGate.cs`, `TODO.md`.
- Issue: #68, split from #11 / #58.
- Non-goals: changing weapon range values, changing deploy semantics, changing damage/cooldown/targeting rules, or merging the combat system family.

Implementation summary:
- Added `WeaponMath.BaseRange(...)` for upgrade-scaled mount range without deploy's positional range multiplier.
- Replaced mobile deploy-aware helpers in `CombatSystem` and `BuildingTargetCombatSystem` with direct `WeaponMath.EffectiveRange(...)` calls.
- Replaced turret and group-attack-slot no-deploy helpers with `WeaponMath.BaseRange(...)`.
- Added `ArchitectureReviewGate` scanning of `scripts/core/sim/systems/**/*.cs` so `private static float WeaponRange(...)` cannot return to sim systems or command partials.
- Left `UnitSpecEntityBridge.WeaponRange(...)` out of scope because it is an authoring bridge helper, not a sim system hot-path helper.

Automated gates:
- Command: `rg "private static float WeaponRange" scripts/core/sim/systems`
  Result: pass
  Evidence: no matches after the refactor.
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite completed successfully; range helper calls are behavior-preserving.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior checks completed successfully, including turret and group combat paths.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture --max-warnings=0`
  Result: pass
  Evidence: ArchitectureReviewGate completed with 0 errors and 0 warnings, including the new weapon range helper rollback check.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: file-size governance completed with 0 errors and 1 warning for `scripts/core/sim/` having 31 C# files.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 1 warning, the same `scripts/core/sim/` directory-shape warning.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, CounterReadabilityQa, and Godot headless QA.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: this locks helper rollback but does not finish the broader long-term combat-system convergence toward one weapon engagement system. File-size governance now warns that `scripts/core/sim/` has 31 C# files; a later directory-shape slice should move a real domain cluster rather than mixing that mechanical move into this helper-gate change.

TODO update:
- Items marked done: `Analyzer/gate for residual debt`.
- Items left open: `Comment discipline` and broad combat-system convergence.
- Reason: the residual gate now covers the three named rollback risks: shared-grid `Cell(...)`, duplicated `WeaponRange(...)` helpers, and unregistered C# files over 600 lines.
