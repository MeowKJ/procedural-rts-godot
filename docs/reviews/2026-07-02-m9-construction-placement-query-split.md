# Review Record - M9 ConstructionSystem placement query split

Step: Split ConstructionSystem placement query helpers into a focused companion file
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: SimReplay / ReviewGate architecture
Integrator AI: Remote Linux Codex

Scope:
- Issue: #67.
- Files/folders: `scripts/core/sim/systems/construction/ConstructionSystem.Queries.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.PlacementQueries.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-construction-placement-query-split.md`.
- Non-goals: changing construction placement rules, build authority semantics, visibility checks, terrain/footprint validation, balance values, UI placement flow, or allocation behavior in this slice.

Implementation summary:
- Moved placement-focused query helpers out of `ConstructionSystem.Queries.cs` into the new partial companion `ConstructionSystem.PlacementQueries.cs`.
- Preserved the existing helper bodies for build anchors, build authority checks, visibility sources, footprint obstacles, and terrain layer lookup.
- Kept the split behavior-preserving so a later Construction placement allocation paydown can work inside a smaller placement-owned file.
- Kept both affected C# files below the 400-line governance threshold: `ConstructionSystem.Queries.cs` is 247 lines and `ConstructionSystem.PlacementQueries.cs` is 119 lines after the split.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including construction-loop, construction-ready-placement, construction-power-gate, construction-visibility, dog-build-authority, deploy-build-authority, and construction lifecycle deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 1 existing source-directory warning for `scripts/core/sim/`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-construction-placement-query-split`
  Result: pass
  Evidence: review-record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, balance/counter QA, and Godot headless QA.

Reviewer result:
- Status: pass.
- Required fixes: none.

Status:
- Complete for #67 after integration gates.

Residual risks:
- This is a structural split only; remaining M9 allocation debt still includes Construction placement-list construction, immutable queue/path arrays, and broader profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #67 prepares the Construction placement query area for later allocation work without closing the broad allocation-debt item.
