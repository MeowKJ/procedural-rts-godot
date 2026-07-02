# Review Record - M9 ConstructionSystem placement validation buffers

Step: Reuse ConstructionSystem placement validation buffers
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: SimReplay / ReviewGate simhot
Integrator AI: Remote Linux Codex

Scope:
- Issue: #70.
- Files/folders: `scripts/core/sim/systems/ConstructionSystem.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Queries.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.PlacementQueries.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-construction-placement-buffers.md`.
- Non-goals: changing placement legality, build authority, visibility, terrain, footprint blocking, ready-ticket behavior, UI placement preview, balance values, or closing the broad #10 allocation paydown item.

Implementation summary:
- Added reusable `ConstructionSystem` buffers for placement build anchors, footprint obstacles, and build visibility sources.
- Changed placement query helpers to fill caller-owned buffers with deterministic `OrderedEntities` loops instead of returning LINQ-built lists.
- Routed `ValidatePlacementArea(...)` through the reusable buffers before calling `PlacementMath.ValidateBuildableArea(...)`.
- Extended broad `ReviewGate simhot` evidence so the Construction placement collectors cannot regress to `IReadOnlyList`/`ToList()` allocation paths.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including construction-loop, dog-build-authority, deploy-build-authority, construction-queue-ready, construction-ready-placement, construction-power-gate, and construction-visibility deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: pass
  Evidence: 400-unit run averaged 10.716ms, p99 11.028ms, max 12.153ms, and 192620 bytes/tick, under the 16.667ms active budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot completed with 0 errors and 0 warnings, including the new Construction placement buffer checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating the validation-tool source budget lock to 17784 total tool lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-construction-placement-buffers`
  Result: pass
  Evidence: review-record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, balance/counter QA, and Godot headless QA. Release PerfSmoke inside VerifyAll reported 400-unit avg 3.240ms and 192620 bytes/tick.

Reviewer result:
- Status: pass.
- Required fixes: none.

Status:
- Complete for #70 after integration gates.

Residual risks:
- This removes one Construction placement allocation family only. Remaining #10 debt still includes ordered prerequisite scans, immutable queue/path arrays, and broader profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #70 narrows one allocation family but does not complete the broad M9 allocation-debt item.
